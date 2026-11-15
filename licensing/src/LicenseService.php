<?php
declare(strict_types=1);
namespace Clanarina\Licensing;

final class LicenseService
{
    private string $store;
    private string $keyPath;

    public function __construct(?string $storage = null, ?string $keyPath = null)
    {
        $base = dirname(__DIR__);
        $this->store = $storage ?? (getenv('CLANARINA_STORAGE_PATH') ?: $base . '/private/storage/data');
        $this->keyPath = $keyPath ?? (getenv('CLANARINA_KEY_FILE') ?: $base . '/private/keys/signing.key');
        if (!is_dir($this->store) && !mkdir($this->store, 0700, true) && !is_dir($this->store)) {
            throw new \RuntimeException('Cannot create private storage');
        }
    }

    private static function b64(string $binary): string
    {
        return rtrim(strtr(base64_encode($binary), '+/', '-_'), '=');
    }
    public static function fromBase64(string $value): string
    {
        if (!preg_match('/^[a-zA-Z0-9_-]+$/D', $value)) {
            throw new \InvalidArgumentException('Invalid base64url');
        }
        $decoded = base64_decode(strtr($value, '-_', '+/') . str_repeat('=', (4 - strlen($value) % 4) % 4), true);
        if ($decoded === false || self::b64($decoded) !== $value) {
            throw new \InvalidArgumentException('Invalid base64url');
        }
        return $decoded;
    }
    private function read(): array
    {
        $path = $this->store . '/licenses.json';
        if (!is_file($path)) return ['licenses' => [], 'attempts' => []];
        $decoded = json_decode((string) file_get_contents($path), true, 512, JSON_THROW_ON_ERROR);
        if (!is_array($decoded) || !isset($decoded['licenses'], $decoded['attempts'])) {
            throw new \RuntimeException('Corrupt license storage');
        }
        return $decoded;
    }
    public function transaction(callable $operation): mixed
    {
        $lock = fopen($this->store . '/licenses.lock', 'c');
        if ($lock === false || !flock($lock, LOCK_EX)) {
            throw new \RuntimeException('Unable to lock license storage');
        }
        try {
            $data = $this->read();
            $result = $operation($data);
            $json = json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_THROW_ON_ERROR);
            $tmp = tempnam($this->store, 'license-');
            if ($tmp === false) throw new \RuntimeException('Unable to allocate storage');
            try {
                chmod($tmp, 0600);
                if (file_put_contents($tmp, $json, LOCK_EX) === false || !rename($tmp, $this->store . '/licenses.json')) {
                    throw new \RuntimeException('Unable to commit license storage');
                }
            } finally {
                if (is_file($tmp)) unlink($tmp);
            }
            return $result;
        } finally {
            flock($lock, LOCK_UN);
            fclose($lock);
        }
    }
    public function initializeKeys(): string
    {
        if (file_exists($this->keyPath)) {
            throw new \RuntimeException('Signing key already exists; refusing to overwrite');
        }
        $dir = dirname($this->keyPath);
        if (!is_dir($dir) && !mkdir($dir, 0700, true)) throw new \RuntimeException('Cannot create key directory');
        $pair = sodium_crypto_sign_keypair();
        $secret = sodium_crypto_sign_secretkey($pair);
        if (file_put_contents($this->keyPath, self::b64($secret), LOCK_EX) === false) {
            throw new \RuntimeException('Unable to save key');
        }
        chmod($this->keyPath, 0600);
        return self::b64(sodium_crypto_sign_publickey($pair));
    }
    public function publicKey(): string
    {
        $secret = $this->privateKey();
        return self::b64(sodium_crypto_sign_publickey_from_secretkey($secret));
    }
    private function privateKey(): string
    {
        if (!is_file($this->keyPath)) throw new \RuntimeException('Signing key is not initialized');
        $secret = self::fromBase64(trim((string)file_get_contents($this->keyPath)));
        if (strlen($secret) !== SODIUM_CRYPTO_SIGN_SECRETKEYBYTES) throw new \RuntimeException('Invalid signing key');
        return $secret;
    }
    public function issue(string $expires, int $seats = 1): array
    {
        if ($seats < 1 || $seats > 10000) throw new \InvalidArgumentException('Invalid seat count');
        $expiration = \DateTimeImmutable::createFromFormat('!Y-m-d', $expires, new \DateTimeZone('UTC'));
        if (!$expiration || $expiration->format('Y-m-d') !== $expires) throw new \InvalidArgumentException('Expected UTC YYYY-MM-DD');
        $id = bin2hex(random_bytes(12));
        $key = 'CLN1-' . strtoupper(bin2hex(random_bytes(16)));
        $record = [
            'id' => $id, 'key_hash' => hash('sha256', $key), 'expires' => $expires,
            'seats' => $seats, 'active' => true, 'installations' => []
        ];
        $this->transaction(function(array &$data) use ($id, $record): void { $data['licenses'][$id] = $record; });
        return ['id' => $id, 'licenseKey' => $key, 'expires' => $expires, 'seats' => $seats];
    }
    public function activate(string $licenseKey, string $installPublicKey, string $platform): array
    {
        if (!preg_match('/^CLN1-[A-F0-9]{32}$/D', $licenseKey)) throw new \InvalidArgumentException('Invalid license');
        if (!in_array($platform, ['windows', 'android'], true)) throw new \InvalidArgumentException('Invalid platform');
        $key = self::fromBase64($installPublicKey);
        if (strlen($key) !== SODIUM_CRYPTO_SIGN_PUBLICKEYBYTES) throw new \InvalidArgumentException('Invalid installation key');
        return $this->transaction(function(array &$data) use ($licenseKey,$installPublicKey,$platform): array {
            $license = null;
            $licenseId = null;
            foreach ($data['licenses'] as $id => &$record) {
                if (hash_equals($record['key_hash'], hash('sha256', $licenseKey))) {
                    $license = &$record; $licenseId = $id; break;
                }
            }
            unset($record);
            if ($license === null || !$license['active']) throw new \InvalidArgumentException('Invalid license');
            $expiry = new \DateTimeImmutable($license['expires'] . 'T23:59:59+00:00');
            if ($expiry < new \DateTimeImmutable('now',new \DateTimeZone('UTC'))) throw new \InvalidArgumentException('Invalid license');
            $installId = hash('sha256', $installPublicKey);
            if (!isset($license['installations'][$installId])) {
                if (count($license['installations']) >= $license['seats']) throw new \InvalidArgumentException('No activation seats available');
                $license['installations'][$installId] = ['platform'=>$platform, 'created_utc'=>gmdate('c')];
            }
            $data['licenses'][$licenseId] = $license;
            return $this->sign([
                'version'=>1,'product'=>'clanarina','licenseId'=>$licenseId,
                'installationPublicKey'=>$installPublicKey,'platform'=>$platform,
                'issuedUtc'=>gmdate('c'),'expiresUtc'=>$expiry->format('Y-m-d\TH:i:s\Z')
            ]);
        });
    }
    private function sign(array $payload): array
    {
        $json = json_encode($payload, JSON_UNESCAPED_SLASHES | JSON_THROW_ON_ERROR);
        return ['payload' => self::b64($json), 'signature' => self::b64(sodium_crypto_sign_detached($json, $this->privateKey()))];
    }
    public static function verify(array $token, string $publicKey): array
    {
        $message = self::fromBase64((string)($token['payload'] ?? ''));
        $signature = self::fromBase64((string)($token['signature'] ?? ''));
        $key = self::fromBase64($publicKey);
        if (strlen($signature)!==SODIUM_CRYPTO_SIGN_BYTES || strlen($key)!==SODIUM_CRYPTO_SIGN_PUBLICKEYBYTES ||
            !sodium_crypto_sign_verify_detached($signature,$message,$key)) throw new \InvalidArgumentException('Invalid signature');
        $payload = json_decode($message, true, 512, JSON_THROW_ON_ERROR);
        if (!is_array($payload) || ($payload['product']??'') !== 'clanarina') throw new \InvalidArgumentException('Wrong product');
        return $payload;
    }
}
