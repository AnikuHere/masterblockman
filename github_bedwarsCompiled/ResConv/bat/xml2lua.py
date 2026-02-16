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
    for node in macro_nodes:
        macros[get_attrvalue(node, 'name')] = int(get_attrvalue(node, 'value'))
    
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
            
            entry['name'] = name
            entry['type'] = type
            if count == '':
                count = 0
            entry['count'] = macros.get(count) or count
                
            struct.append(entry)
                
        structs[get_attrvalue(snode, 'name')] = struct

    return structs

def load_conv_list():
    conv_list = []
    root = load_xml('../convlist/convlist_Moba.xml')
    conv_nodes = get_xmlnode(root, 'ConvTree')
    comm_nodes = get_xmlnode(conv_nodes[0], 'CommNode')
    for cnode in comm_nodes:
        res_nodes = get_xmlnode(cnode, 'ResNode')
        for rnode in res_nodes:
            meta = get_attrvalue(rnode, 'Meta')
            bin_file = get_attrvalue(rnode, 'BinFile')
            conv_list.append({
                'meta': meta,
                'name': bin_file.replace('.bytes', ''),
                'xml': bin_file.replace('.bytes', '.xml'),
                'lua': bin_file.replace('.bytes', '.lua')
            })
    return conv_list

def is_number_type(type):
    return type[:3] == 'int' or type == 'char' or type == 'smallint' or type == 'long'

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
    
    f = open(out_file, "wb")
    f.write(utf8)
    f.close()

def escape_lua_string(str_value):
    str_value = str_value.replace('"', '\\"')
    str_value = str_value.replace('\n', '\\\n')
    return '"' + str_value + '"'

def to_lua_string(obj, depth = 0):
    if isinstance(obj, unicode) or isinstance(obj, str):
        return escape_lua_string(obj)
    
    if isinstance(obj, list):
        seq = []
        for item in obj:
            seq.append(to_lua_string(item, depth + 1))
        return '{' + ', '.join(seq) + '}'
    
    if isinstance(obj, dict):
        prefix = "  " * depth
        seq = []
        for key in obj:
            value = obj[key]
            seq.append('\n  ' + prefix + key + ' = ' + to_lua_string(value, depth + 1))
        return '{' + ', '.join(seq) + '\n' + prefix + '}'
    
    return str(obj)

def main():
    bin_path = '../bin/'
    out_path = '../../../blockunity-server/logicserver/lualib/res/'
    structs = load_ds_res()
    conv_list = load_conv_list()
    for node in conv_list:
        name = node['name']
        xml = node['xml']
        meta = node['meta']
        
        try:
            print 'Convert-> ', name
            lua_str = name + ' = ' + to_lua_string(load_xml_res(bin_path + xml, meta, structs))
            save_file(out_path + node['lua'], lua_str)
        except IOError as err:
            print(err)

main()
