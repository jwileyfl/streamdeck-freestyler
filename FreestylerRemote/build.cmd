@ECHO OFF
CLS
TITLE Building distribution package

SET TGT=DistributionPackage

IF NOT "%1"=="" (
    SET BUILD=%1
) ELSE (
    SET BUILD=Release
)

IF EXIST %TGT%\ (
    DEL /s /f /q %TGT%
) ELSE (
    MD %TGT%
)

IF NOT EXIST bin\ (
	MD bin
)

ECHO DistributionTool.exe -b -i bin\%BUILD%\com.resnexsoft.freestyler.remote.sdPlugin -o %TGT%
DistributionTool.exe -b -i bin\%BUILD%\com.resnexsoft.freestyler.remote.sdPlugin -o %TGT%