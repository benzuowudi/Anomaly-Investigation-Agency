set WORKSPACE=..\..

set GEN_CLIENT=%WORKSPACE%\Tools\LuBan\Luban.ClientServer\Luban.ClientServer.exe
set CONF_ROOT=%WORKSPACE%\Tools\LuBan\MiniTemplate


%GEN_CLIENT% -j cfg --^
 -d %CONF_ROOT%\Defines\__root__.xml ^
 --input_data_dir %WORKSPACE%\DataTable ^
 --output_code_dir %WORKSPACE%\Assets\HotUpdate\GameMain\Script\DataTable ^
 --output_data_dir %WORKSPACE%\Assets\StreamingAssets\Data ^
 --gen_types code_cs_unity_json,data_json ^
 -s all 

pause