cd ./../tresconv/bin/

del ResConvert.log

::通过转表工具生成前台需要的bin文件
ResConvert.exe /convert ./../../convlist/convlist_Language.xml UTF-8

::显示日志
type ResConvert.log


pause
