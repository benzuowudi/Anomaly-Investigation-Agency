项目数据

常用文字颜色
白色文字颜色	E1DDE9
浅蓝色文字颜色	6A96DE
深蓝色文字颜色	005982
置灰颜色 	bfbfc1

中文全角空格	“　”

红色文字颜色	eb414e
橙色文字颜色	ed9c1c
紫色文字颜色	cd40e7
蓝色文字颜色	3d66eb
绿色文字颜色 	43ee94


文本中英对照
详情		Details
彩票		Lottery
仓库/库存	Inventory
资料		Material
成就		Achievement
同伴		Companion
宿舍		Dormitory
日历		Calendar
指南		Guide


项目操作流程

LuBan读取流程
将对应的配置表添加到DataTable文件夹中
需要在__tables__.xlsx中进行标注，将配置表名称，json文件名称，类名配置好
在unity中使用LuBanTool进行打包操作
完成后可以通过GameInit.Data.GetData.表名获取到对应的结构体

ui添加流程
制作ui预制件
在DataTable文件夹中的ui.xlsx中添加对应的UI名称
ui对应的uiwindow脚本
ui预制件打包
更新ab包资源

数据资源读取流程
从data中读取配置表名称配置
按照data中的数据将配置表数据依次存储为textasset文件
在模块需要调用配置表数据时，使用配置表名称寻找到对应的textasset文件并进行xml转换
将转换的xml文件中的数据封装为结构体进行传递与使用

加载操作逻辑
在进入游戏场景后，如果有需求则需要通过Operate加载一个GameOperate到场景中
新增一个GameOperate需要创建名称相同的空对象，并将空对象制作成预制件打包
GameOperate同时最多允许存在1个，每一次加载新的GameOperate都会将之前的卸载掉

项目注意事项

1，因为所有的UI资源都已经打包，所以在加载UI的时候需要先加载对应的AB包资源



