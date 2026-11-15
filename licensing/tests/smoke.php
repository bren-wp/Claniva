<?php
declare(strict_types=1);
require dirname(__DIR__) . '/src/LicenseService.php';
use Clanarina\Licensing\LicenseService;
$root=sys_get_temp_dir().'/clanarina-test-'.bin2hex(random_bytes(5));
mkdir($root,0700,true);
try {
    $svc=new LicenseService($root.'/store',$root.'/private/sign.key');
    $pub=$svc->initializeKeys();
    $issued=$svc->issue('2099-12-31',1);
    $pair=sodium_crypto_sign_keypair();
    $install=rtrim(strtr(base64_encode(sodium_crypto_sign_publickey($pair)),'+/','-_'),'=');
    $token=$svc->activate($issued['licenseKey'],$install,'android');
    $decoded=LicenseService::verify($token,$pub);
    if ($decoded['installationPublicKey']!==$install) throw new RuntimeException('Install binding mismatch');
    if ($svc->activate($issued['licenseKey'],$install,'android')['signature']==='') throw new RuntimeException('Repeat activation invalid');
    $another=sodium_crypto_sign_keypair();
    $other=rtrim(strtr(base64_encode(sodium_crypto_sign_publickey($another)),'+/','-_'),'=');
    try { $svc->activate($issued['licenseKey'],$other,'windows'); throw new RuntimeException('Seat overflow was accepted'); }
    catch(InvalidArgumentException $expected) {}
    $token['signature']=str_repeat('A',strlen($token['signature']));
    try { LicenseService::verify($token,$pub); throw new RuntimeException('Forged signature accepted'); }
    catch(InvalidArgumentException $expected) {}
    echo "PASS: licensing smoke tests\n";
} finally {
    foreach (glob($root.'/store/*') ?: [] as $p) unlink($p);
    if(is_dir($root.'/private')) { unlink($root.'/private/sign.key'); rmdir($root.'/private'); }
    if(is_dir($root.'/store')) rmdir($root.'/store');
    rmdir($root);
}
