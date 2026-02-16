echo off
.\tdr.exe -H -p -l -o Ds_Res.h ..\xml\Ds_Res.xml
if exist ..\xml\Ds_Res.h @echo Ds_Res.h文件到Include目录下 
if exist ..\xml\Ds_Res.h xcopy/Y ..\xml\Ds_Res.h ..\Include

.\tdr.exe -H -p -l -o keywords_Res.h ..\xml\keywords_Res.xml
if exist ..\xml\keywords_Res.h @echo keywords_Res.h文件到Include目录下 
if exist ..\xml\keywords_Res.h xcopy/Y ..\xml\keywords_Res.h ..\Include

.\tdr.exe -P -p -l -o Ds_Config.h ..\xml\Ds_Config.xml
if exist ..\xml\Ds_Config.h xcopy/Y ..\xml\Ds_Config.h ..\Include
if exist ..\xml\Ds_Config.h @echo Ds_Config.h文件到Include目录下 

.\tdr.exe -P -p -l -o CSBattleProtocol.h ..\xml\CSBattleProtocol.xml ..\xml\CSBattleComm.xml
if exist ..\xml\CSBattleProtocol.h xcopy/Y ..\xml\CSBattleProtocol.h ..\Include
if exist ..\xml\CSBattleProtocol.h @echo CSBattleProtocol.h文件到Include目录下 


if exist ..\xml\Ds_Res.h xcopy/Y ..\xml\Ds_Res.h ..\..\..\blockunity-server\battleserver\Dsloader\ResHeader\
if exist ..\xml\keywords_Res.h xcopy/Y ..\xml\keywords_Res.h  ..\..\..\blockunity-server\battleserver\Dsloader\ResHeader\

if exist MobaConfigDBMeta del /S /Q MobaConfigDBMeta\Tdr*.*
if exist BattleProtocol del /S /Q BattleProtocol\Tdr*.*

if exist MobaConfigDBMeta xcopy/Y MobaConfigDBMeta ..\Include\MobaConfigDBMeta\
if exist MobaConfigDBMeta xcopy/Y ..\Include\MobaConfigDBMeta\*.*   ..\..\..\blockunity-server\battleserver\Dsloader\ResHeader\

if exist BattleProtocol xcopy/Y BattleProtocol ..\Include\BattleProtocol\
if exist BattleProtocol xcopy/Y ..\Include\BattleProtocol\CSBattle*.*  ..\..\..\blockunity-server\battleserver\Dsloader\ResHeader\
if exist BattleProtocol xcopy/Y ..\Include\BattleProtocol\BattleProtocol_metalib.h ..\..\..\blockunity-server\battleserver\Dsloader\ResHeader\

pause



