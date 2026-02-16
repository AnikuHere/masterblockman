cd ./../tresconv/bin/

del ResConvert.log

::通过转表工具生成前台需要的bin文件
ResConvert.exe /convert ./../../convlist/convlist_MOBA.xml UTF-8

::显示日志
type ResConvert.log

xcopy ..\..\bin\*.bytes ..\..\..\UnityProj\Assets\Resources\data\bin\ /y


pause
