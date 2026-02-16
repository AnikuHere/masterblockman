#!/usr/bin/env python
# by Rick
import __future__
import tokenize
import os
import sys
import xml.dom.minidom
import codecs

def get_xmlnode(node, name):
    return node.getElementsByTagName(name) if node else []

def get_attrvalue(node, attrname):
    return node.getAttribute(attrname) if node else ''

def get_nodevalue(node, index = 0):
    if len(node.childNodes) > index: # empty node has no childNodes
        return node.childNodes[index].nodeValue
    return ''

def load_xml(xml_file, code = 'gbk'):
    f = open(xml_file, "r")
    str = f.read()
    f.close()
    if code == 'gbk':
        str = str.replace('<?xml version="1.0" encoding="GBK" standalone="yes" ?>','<?xml version="1.0" encoding="utf-8" standalone="yes" ?>')
        str = unicode(str, encoding=code).encode('utf-8')
    xmldoc = xml.dom.minidom.parseString(str)
    return xmldoc.documentElement
	
def load_ds_res():
    root = load_xml('../xml/Ds_Res.xml')
    macro_nodes = get_xmlnode(root, 'macro')
    macros = {}
    #for node in macro_nodes:
		#macros[get_attrvalue(node, 'name')] = 'MobaResDBMetaMacros.' + get_attrvalue(node, 'name')
        #macros[get_attrvalue(node, 'name')] = int(get_attrvalue(node, 'value'))
    
    struct_nodes = get_xmlnode(root, 'struct')
    structs = {}
    for snode in struct_nodes:
        struct = []
        entry_nodes = get_xmlnode(snode, 'entry')
        for enode in entry_nodes:
            entry = {}
            name = get_attrvalue(enode, 'name')
            type = get_attrvalue(enode, 'type')
            count = get_attrvalue(enode, 'count')
            desc = get_attrvalue(enode, 'desc')
			
            entry['name'] = name
            entry['type'] = type
            entry['desc'] = desc
            if count == '':
                count = 0
            else:
				count = 'MobaResDBMetaMacros.' + count
            entry['count'] = count
                
            struct.append(entry)
                
        structs[get_attrvalue(snode, 'name')] = struct

    return structs



def check_data_type(type):
	if type == 'int8' or type == 'int' or type == 'int64' or type == 'string' or type == 'int16' or type == 'char' or type == 'int32' or type == 'smallint':
		return True
	else:
		return False

def return_data_type(type):
	if type == 'string':
		return ''
	if check_data_type(type):
		return str(0)
	return 'null'
	
def check_string_type(type):
	if type == 'string':
		return True
	else:
		return False

def load_single_struct(structName, struct):
	struct_str = '\tpublic class ' + structName + 'Export : ResExport\n\t{\n\t\tprivate ' + structName + ' m_' + structName + ' = null;\n'
	struct_uninit = '\t\tpublic void OnUnInit()\n\t\t{\n\t\t\tm_' + structName + ' = null;\n'
	for entry in struct:
		type = entry['type']
		name = entry['name']
		if check_data_type(type):
			if check_string_type(type):
				struct_str = struct_str + '\t\tprivate ' + type + ' m_sz' + name + ' = null;\n'
				struct_uninit = struct_uninit + '\t\t\tm_sz' + name + ' = null;\n'
			else:
				continue
		else:
			count = entry['count']
			if count == 0:
				struct_uninit = struct_uninit + '\t\t\tm_' + name[:1].upper() + name[1:] + ' = null;\n'
				struct_str = struct_str + '\t\tprivate ' + type + 'Export m_' + name[:1].upper() + name[1:] + ' = null;\n'
			else:
				struct_uninit = struct_uninit + '\t\t\tm_' + name[:1].upper() + name[1:] + 's = null;\n'
				struct_str = struct_str + '\t\tprivate ' + type + 'Export [] m_' + name[:1].upper() + name[1:] + 's = null;\n'
	struct_uninit = struct_uninit + '\t\t}\n\n'
	struct_str = struct_str +  load_base_func(structName, struct) + struct_uninit + load_get_func(structName, struct) + '\t}\n\n'
	return struct_str

def load_base_func(structName, struct):
	struct_init = '\t\tpublic void OnInit(' + 'object'  + ' ' +  structName + '_res)\n\t\t{\n\t\t\tm_' + structName + ' = (' + structName + ')' + structName + '_res;\n'
	struct_new = '\t\tpublic ' + structName + 'Export ()\n\t\t{\n'
	struct_new_body = ''
	for entry in struct:
		type = entry['type']
		name = entry['name']
		if check_data_type(type):
			if check_string_type(type):
				struct_init = struct_init + '\t\t\tm_sz' + name + ' = StringHelper.GetString(m_' + structName + '.sz' + name[:1].upper() + name[1:] + ');\n\t\t\tm_' + structName + '.sz' + name[:1].upper() + name[1:] + ' = null;\n'
			else:
				continue
		else:
			count = entry['count']
			if count == 0:
				struct_init = struct_init + '\t\t\tm_' + name[:1].upper() + name[1:] + '.OnInit(m_' + structName + '.st' + name[:1].upper() + name[1:] + ');\n'
				struct_new_body = struct_new_body + '\t\t\tm_' + name[:1].upper() + name[1:] + ' = new ' + type + 'Export();\n'
			else:
				struct_init = struct_init + '\t\t\tfor(int i=0; i<' + count + '; i++)\n\t\t\t{\n\t\t\t\tm_' + name[:1].upper() + name[1:] + 's[i].OnInit(m_' + structName + '.ast' + name[:1].upper() + name[1:] + '[i]);\n\t\t\t}\n'
				struct_new_body = struct_new_body + '\t\t\tm_' + name[:1].upper() + name[1:] + 's = new ' + type + 'Export[' + count + '];\n\t\t\t' + 'for(int i=0; i<' + count + '; i++)\n\t\t\t{\n\t\t\t\tm_' + name[:1].upper() + name[1:] + 's[i] = new ' + type + 'Export();\n\t\t\t}\n'
	struct_init = struct_init + '\t\t}\n\n'
	if struct_new_body == '':
		return struct_init
	else:
		return struct_new + struct_new_body + '\t\t}\n\n' + struct_init 
		
def cast_type_name(type):
	if type == 'int' or type == 'int32':
		return 'Int32'
	if type == 'int64':
		return 'Int64'
	if type == 'int8' or type == 'char':
		return 'sbyte'
	if type == 'int16' or type == 'smallint':
		return 'Int16'
	return type

def check_char_type(type):
	if type == 'int8' or type == 'char':
		return True
	return False

def cast_name(type, name):
	if type == 'int' or type == 'int32':
		return 'i' + name[:1].upper() + name[1:]
	if type == 'int64':
		return 'll' + name[:1].upper() + name[1:]
	if type == 'int8' or type == 'char':
		return 'ch' + name[:1].upper() + name[1:]
	if type == 'int16' or type == 'smallint':
		return 'n' + name[:1].upper() + name[1:]
	return type
		
def load_get_func(structName, struct):
	get_str = ''
	for entry in struct:
		name = entry['name']
		type = cast_type_name(entry['type'])
		count = entry['count']
		desc = entry['desc']
		name_type = cast_name(entry['type'], name)
		fun_desc = '\t\t//' + desc+'\n'
		if count == 0:
			if check_data_type(entry['type']):
				if check_string_type(entry['type']):
					get_str = get_str + fun_desc +'\t\tpublic ' + type + ' Get' + name[:1].upper() + name[1:] + '()\n\t\t{\n\t\t\treturn m_sz' + name + ';\n\t\t}\n\n'					
					if name.find('_SCN') > -1:
						languageFun = name.replace('_SCN','')
						get_str = get_str + fun_desc +'\t\tpublic ' + type + ' Get' + languageFun[:1].upper() + languageFun[1:]+''\
						+'()\n\t\t{\n\t\t\t LanguageDefine language = ResourceMgrExport.instance.GetLanguage();'\
						+'\n\t\t\tif(language==LanguageDefine.SIM_CHINESE)\n\t\t\t{\n\t\t\t\treturn m_sz' + languageFun + '_SCN;\n\t\t\t}'\
						+'\n\t\t\telse if(language==LanguageDefine.FON_CHINESE)\n\t\t\t{\n\t\t\t\treturn m_sz' + languageFun + '_FCN;\n\t\t\t}'\
						+'\n\t\t\telse if(language==LanguageDefine.ENGLISH)\n\t\t\t{\n\t\t\t\treturn m_sz' + languageFun + '_EN;\n\t\t\t}'\
						+'\n\t\t\telse\n\t\t\t{\n\t\t\t\treturn m_sz' + languageFun + '_SCN;\n\t\t\t}'\
						+'\n\t\t}\n\n'
				else:
					get_str = get_str + fun_desc + '\t\tpublic ' + type + ' Get' + name[:1].upper() + name[1:] + '()\n\t\t{\n\t\t\treturn m_' + structName + '.' + name_type + ';\n\t\t}\n\n'
					get_str = get_str + fun_desc + '\t\tpublic ' + type + ' ' + name_type + '\n\t\t{\n\t\t\tget{ return m_' + structName + '.' + name_type + ';}\n\t\t}\n\n'
			else:
				get_str = get_str + fun_desc + '\t\tpublic ' + type + 'Export Get' + name[:1].upper() + name[1:] + '()\n\t\t{\n\t\t\treturn m_' + name[:1].upper() + name[1:] + ';\n\t\t}\n\n'
		else:
			if check_data_type(entry['type']):
				return_value = return_data_type(entry['type'])
				if check_char_type(entry['type']):
					get_str = get_str + fun_desc + '\t\tpublic ' + type + ' Get' + name[:1].upper() + name[1:] + '(int index)\n\t\t{\n\t\t\tif(index<0 || index>=' + count + ')\n\t\t\t\treturn ' + return_value +  ';\n\t\t\treturn m_' + structName + '.sz' + name[:1].upper() + name[1:] + '[index];\n\t\t}\n\n'
					get_str = get_str + '\t\tpublic ' + 'int Get' + name[:1].upper() + name[1:] + '_Length()\n\t\t{\n\t\t\treturn ' + count +  ';\n\t\t}\n\n'
				else:
					get_str = get_str + fun_desc + '\t\tpublic ' + type + ' Get' + name[:1].upper() + name[1:] + '(int index)\n\t\t{\n\t\t\tif(index<0 || index>=' + count + ')\n\t\t\t\treturn ' + return_value +  ';\n\t\t\treturn m_' + structName + '.' + name + '[index];\n\t\t}\n\n'
					get_str = get_str + '\t\tpublic ' + 'int Get' + name[:1].upper() + name[1:] + '_Length()\n\t\t{\n\t\t\treturn ' + count +  ';\n\t\t}\n\n'
			else:
				get_str = get_str + fun_desc + '\t\tpublic ' + type + 'Export Get' + name[:1].upper() + name[1:] + '(int index)\n\t\t{\n\t\t\tif(index<0 || index>=' + count + ')\n\t\t\t\treturn null;\n\t\t\treturn m_' + name[:1].upper() + name[1:] + 's[index];\n\t\t}\n\n'
				get_str = get_str + '\t\tpublic ' + 'int Get' + name[:1].upper() + name[1:] + '_Length()\n\t\t{\n\t\t\treturn ' + count +  ';\n\t\t}\n\n'
	return get_str
	

def load_single_value(item, type, structs):
    if is_number_type(type):
        return int(get_nodevalue(item))
        
    if type == "string":
        return get_nodevalue(item)
    
    result = {}
    meta = structs[type]
    for entry in meta:
        name = entry['name']
        count = int(entry['count'])
        
        sub_nodes = get_xmlnode(item, name)
        value = load_value(sub_nodes, entry['type'], count, structs)
        
        result[name] = value
        
    return result

def load_value(items, type, count, structs):
    if count == 0:
        return load_single_value(items[0], type, structs)
        
    if is_number_type(type):
        seq = []
        for s in get_nodevalue(items[0]).split():
            seq.append(int(s))
        return seq
        
    if type == "string":
        return get_nodevalue(items[0]).split()

    if len(items) != count:
        print 'load_value failed! len(items) != count', type, len(items), count
        raise Exception('res data error!')
        
    result = []
    for item in items:
        result.append(load_single_value(item, type, structs))

    return result

def load_xml_res(xml_file, type, structs):
    root = load_xml(xml_file, 'utf-8')
    items = get_xmlnode(root, type)
    return load_value(items, type, len(items), structs)

def save_file(out_file, text):
    utf8 = text.encode('utf-8')
    if utf8[3:] == codecs.BOM_UTF8:
        utf8 = utf8[:3]
    
    f = open(out_file, "w")
    f.write(utf8)
    f.close()



def main():
    bin_path = '../bin/'
    out_path = '../include/Ds_ResExport.cs'
    structs = load_ds_res()
    str = '/* This file is generated by tdr. */\n/* No manual modification is permitted. */\n\nusing System;\nusing System.Diagnostics;\nusing System.Collections.Generic;\nusing System.Runtime.InteropServices;\nusing System.Text;\nusing tsf4g_tdr_csharp;\n\nnamespace MobaResDBMeta\n{\n\tpublic interface ResExport\n\t{\n\t\tvoid OnInit(object _object);\n\t\tvoid OnUnInit();\n\t}\n\n'
    for name in structs:
		str = str + load_single_struct(name, structs[name])
    print 'Convert-> ', xml 
    str = str + '\n}'    
    save_file(out_path , str)

main()
