<?php
declare(strict_types=1);
header('Content-Type: application/json; charset=utf-8');
header('Cache-Control: no-store');
header('X-Content-Type-Options: nosniff');
function respond(int $status, array $body): never {
    http_response_code($status);
    echo json_encode($body, JSON_UNESCAPED_SLASHES);
    exit;
}
if (($_SERVER['REQUEST_METHOD'] ?? '') !== 'POST') respond(405,['error'=>'Method not allowed']);
if (($_SERVER['HTTPS'] ?? '') !== 'on') respond(403,['error'=>'HTTPS required']);
if ((int)($_SERVER['CONTENT_LENGTH'] ?? 0) > 8192) respond(413,['error'=>'Request too large']);
$input = json_decode((string)file_get_contents('php://input'),true);
if (!is_array($input)) respond(400,['error'=>'Invalid JSON']);
$key = $input['licenseKey'] ?? null;
$public = $input['installationPublicKey'] ?? null;
$platform = $input['platform'] ?? null;
if (!is_string($key) || !is_string($public) || !is_string($platform) ||
    strlen($key)>128 || strlen($public)>128 || strlen($platform)>32) {
    respond(400,['error'=>'Invalid input']);
}
require dirname(__DIR__,2) . '/src/LicenseService.php';
try {
    $service = new \Clanarina\Licensing\LicenseService();
    $secret = getenv('CLANARINA_THROTTLE_SECRET');
    if (!is_string($secret) || strlen($secret)<32) respond(503,['error'=>'Licensing is not configured']);
    $identifier = hash_hmac('sha256',(string)($_SERVER['REMOTE_ADDR']??'unknown'),$secret);
    $permitted = $service->transaction(function(array &$data) use ($identifier): bool {
        $now=time();
        foreach ($data['attempts'] as $k=>$attempt) {
            if (($attempt['reset']??0)<$now) unset($data['attempts'][$k]);
        }
        $entry=$data['attempts'][$identifier]??['count'=>0,'reset'=>$now+900];
        if ($entry['count']>=20) return false;
        $entry['count']++;
        $data['attempts'][$identifier]=$entry;
        return true;
    });
    if (!$permitted) respond(429,['error'=>'Too many requests']);
    $token=$service->activate($key,$public,$platform);
    respond(200,['license'=>$token]);
} catch(\InvalidArgumentException $error) {
    respond(400,['error'=>'Activation unsuccessful']);
} catch(\Throwable $error) {
    error_log('Clanarina activation error: ' . get_class($error));
    respond(503,['error'=>'Service unavailable']);
}
