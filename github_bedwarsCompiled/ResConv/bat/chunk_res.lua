--[[
detail: 后处理tdr生成的协议和数据代码文件
--]]

function readfile(filename)
	local file = io.open(filename, "r")
	assert(file)
	local source = file:read("*a")
	file:close()

	return source;
end

function savefile(filename, source)
	local file = io.open(filename, "w")
	assert(file)
	file:write(source)
	file:close()
end

function removemethod(methodname, source)
    --在tsf4g_csharp_interface中去掉
    source = string.gsub(source, "%c    TdrError.ErrorType " .. methodname .. "%(%C-%);", "")

    --在各个class中去掉
	source = string.gsub(source, "%c    TdrError.ErrorType " .. methodname .. "%(.-%)%c    {.-%c    }%c", "")
    source = string.gsub(source, "%c    public TdrError.ErrorType " .. methodname .. "%(.-%)%c    {.-%c    }%c", "")

    --去掉注释
    source = string.gsub(source, "%c    /%*   " .. methodname .. " function %*/", "")

	return source;
end

function removeClass(classname, source)
    
    --source = string.gsub(source, "public interface " .. classname .. "%c{.-%c    }%c", "")
	
	 source = string.gsub(source, ""..classname, ""..classname.."_UNUSEED")
	return source;
end

function DoMain()

   local filelist = {
	[1] = "../include/keywords_Res.cs";
   };
   
   for i=1,#filelist do
   
		--读取协议文件
		local source = readfile(filelist[i])
		
		--去掉tdr生成的错误代码块：
		--[[
			{
				return %d;
			}
		--]]
		--source = string.gsub(source, "%c    {%c        return %d+;%c    }", "")

		--去掉不需要的接口
		--source = string.gsub(source, "%c: IPackable, IUnpackable", "")

		
		source = removeClass("tsf4g_csharp_interface", source);

		--保存协议文件
		savefile(filelist[i], source);
	end
	
	local source2 = readfile("../include/Ds_Config.cs");
	source2 = string.gsub(source2, "byte%[%]%[%]", "public byte[][]");
	savefile("../include/Ds_Config.cs", source2);

	local source3 = readfile("../include/Ds_Res.cs");
	source3 = string.gsub(source3, "public class", "internal class")
	savefile("../include/Ds_Res.cs", source3);
	
	local battle_comm = "../include/CSBattleComm.cs"
	local source = readfile(battle_comm)
	source = string.gsub(source, "new TdrWriteBuf", "TdrWriteBuf.Ins.set")
	source = string.gsub(source, "new TdrReadBuf", "TdrReadBuf.Ins.set")
	savefile(battle_comm, source);

	local battle_protocol = "../include/CSBattleProtocol.cs"
	source = readfile(battle_protocol)
	source = string.gsub(source, "new TdrWriteBuf", "TdrWriteBuf.Ins.set")
	source = string.gsub(source, "new TdrReadBuf", "TdrReadBuf.Ins.set")
	savefile(battle_protocol, source);
	
	
	
end

DoMain();


