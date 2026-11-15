<?php
declare(strict_types=1);
require dirname(__DIR__) . '/src/LicenseService.php';
use Clanarina\Licensing\LicenseService;
if (PHP_SAPI !== 'cli') exit(1);
$service = new LicenseService();
$command = $argv[1] ?? '';
try {
    switch($command) {
        case 'init':
            echo "Public key (embed in native verification after secure enrollment):\n" . $service->initializeKeys() . "\n";
            break;
        case 'public':
            echo $service->publicKey() . "\n";
            break;
        case 'issue':
            echo json_encode($service->issue((string)($argv[2]??''),(int)($argv[3]??1)), JSON_PRETTY_PRINT|JSON_THROW_ON_ERROR) . "\n";
            break;
        case 'offline':
            // Usage: php license.php offline CLN1-... request.json
            $request = json_decode((string)file_get_contents((string)($argv[3]??'')),true,512,JSON_THROW_ON_ERROR);
            $token = $service->activate((string)($argv[2]??''),(string)($request['installationPublicKey']??''),(string)($request['platform']??''));
            echo json_encode($token,JSON_PRETTY_PRINT|JSON_THROW_ON_ERROR) . "\n";
            break;
        default:
            fwrite(STDERR,"Commands: init | public | issue YYYY-MM-DD [seats] | offline CLN1-... request.json\n");
            exit(2);
    }
} catch(Throwable $error) {
    fwrite(STDERR,"Operation failed: " . $error->getMessage() . "\n");
    exit(1);
}
