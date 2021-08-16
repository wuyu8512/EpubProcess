#!/usr/bin/env python
# -*- coding: utf-8 -*-

import sys,os,time
import re

ZHUBIAO = '※' # 自定义注标符号，可标定注标的位置

#注释正则书写规则：r'((?:注标)(正文))?(注头(?:[间隔符]?))(注释内容)

#注释类型 0（位于段落内部）
#匹配类型：.....（注：注释内容）.....
MODE_0_EXP = r'((.{0}))?[〈（\(]([^（）()〈〉]{0,5}[註注][释釋]?(?:[\d]+)?[:：︰\s\u3000])((?:[^()（）〈〉]|[（\(](?:[^()（）])*[）\)])*)[\)）〉]'

#注释类型 1（位于段落内部）
#匹配类型：.....（注：注释内容）.....   或   ...※...（注：注释内容）....   或   ...<sup>※</sup>...（注：注释内容）....
MODE_1_EXP = r'((?:(?:<sup>)?'+ZHUBIAO+r'(?:</sup>)?)(.*?))?[（(]([^（）()]{0,5}[註注][释釋]?(?:[\d]+)?[:：︰\s\u3000])((?:[^()（）]|[（(](?:[^()（）])*[）)])*)[)）]'

#注释类型 2（角川脚注，位于页面底部）
MODE_2_EXP = r'([（\(]?(?:<a.*?class="cyu".*?><span.*?class="key\d*?".*?>[\(（]?[^\(（）\)\n]{0,5}[註注].*?[\)）]?.*?</a>[）\)]?)([\s\S]*?))<(?:div|p) id=".*?>※?<a.*? class="cyu".*?>[\s　]*(.*?)</a>(.*?)</(?:div|p)>'

#注释类型 3（尖端脚注，位于页面底部）
MODE_3_EXP = r'((?:（?[注註]?<a class="footnote-link".*?</a>）?)([\s\S]*?))<p>[\s\u3000]*?(<a class="footnote-anchor".*?>.*?</a>)(.*?)(?:<br/>)?</p>'

#注释类型 4（青文脚注，位于页面底部）
MODE_4_EXP = r'((?:（?(?:<su[bp]>)?<a class="footnote" href="#fnX.*?</a>(?:</su[bp]>)?)([\s\S]*?))<p.*?id="fnX.*?>[\s\u3000]*?([\d\uff10-\uff19注註]*?[:：︰\s\u3000]*?)(.*?)[\n\t ]*?<a href="#fX.*?</a></p>'

notes_arr = []
count = 0

def notes_replace(MODE,epub):

    global count,notes_arr
    is_notes_detected = 0

    def func_(match_):
        global count
        count += 1
        # m0为注标与注释文本之间多出的正文部分
        m0 = ""
        if match_.group(1) != None:
            m0 = match_.group(2)
        # m1 为注释引词、注词等
        m1 = re.sub(r'<a.*?>(.*?)</a>',r'\1',match_.group(3))
        # m2 为注释内容
        m2 = match_.group(4)
        # 注标转化为图标
        result = '<a class="duokan-footnote" epub:type="noteref" href="#note{num}" id="r_note{num}"><sup><img alt="note" src="../Images/note.png"/></sup></a>{m0}'.format(num=str(count),m0=m0,m1=m1,m2=m2)
        # 储存注释内容信息
        notes_arr.append([m1,m2,str(count)])
        return result
    
    for html_id in epub.GetTextIDs():
        filename = os.path.basename(epub.GetEntryName(html_id))
        html = epub.GetItemContentByID(html_id)
        #检索现有的注释，如果存在，则count改为当前页面注释id的最大序号。
        id_num = re.findall(r'<aside.*?id="note(\d+?)".*?>',html)
        if id_num != []:
            id_num.sort()
            count = int(id_num[-1])
        #开始进行处理
        while 1:
            html_ = re.sub(MODE,func_,html,1)
            if html != html_:
                html = html_
            else:
                break
        if notes_arr != []:
            is_notes_detected = 1
            print('-'*45+'\n在文件 '+filename+' 发现注释内容：\n')
            for m1,m2,num in notes_arr:
                #将注释内容转化
                html = re.sub(r'(id="r_note{0}".*?</p>(?:[\r\n\t ]*<aside[\s\S]+?</aside>)*)'.format(num),r'\1\n<aside epub:type="footnote" id="note{num}">\n\t<a href="#r_note{num}"></a>\n\t<ol class="duokan-footnote-content" style="list-style:none">\n\t\t<li class="duokan-footnote-item" id="note{num}">{m1}{m2}</li>\n\t</ol>\n</aside>'.format(num=num,m1=m1,m2=m2),html)
                print(m1+m2+'\n')
            epub.SetItemContentByID(html_id,html)
        notes_arr = []
    if not is_notes_detected:
        print('未发现该类型注释内容')
    
    #return is_notes_detected
    return 0

def run(epub):
    #start = time.time()

    print('\n欢迎使用BW书源注释处理插件：\n\n'+
          '本插件可处理轻小说书源epub中包括尾注、脚注等多种类型的注释。\n\n'+
          '使用本插件需要注意：\n\n'+
          '1. 页底注释处理后可能残留分隔线、边框等html代码，需要手动清理；\n\n'+
          '2. 如原文中存在※号注标的注释，请手动调节注释图标位置，并清除※号。')

    is_notes_detected = 0

    if not is_notes_detected:
        print('\n\n'+'#'*45+'\n正在使用 注释类型 1 (通用)规则处理文本\n'+'#'*45+'\n')
        is_notes_detected = notes_replace(MODE_0_EXP,epub)
    if not is_notes_detected:
        print('\n\n\n'+'#'*45+'\n正在使用 注释类型 2 (角川脚注)规则处理文本\n'+'#'*45+'\n')
        is_notes_detected = notes_replace(MODE_2_EXP,epub)
    if not is_notes_detected:
        print('\n\n\n'+'#'*45+'\n正在使用 注释类型 3 (尖端脚注)规则处理文本\n'+'#'*45+'\n')
        is_notes_detected = notes_replace(MODE_3_EXP,epub)
    if not is_notes_detected:
        print('\n\n\n'+'#'*45+'\n正在使用 注释类型 4 (青文脚注)规则处理文本\n'+'#'*45+'\n')
        is_notes_detected = notes_replace(MODE_4_EXP,epub)

    #end = time.time()
    #print("\n\n"+"-"*45+"\n处理完毕，程序处理时间:%.5f秒"%(end-start))
    print("\n\n"+"-"*45+"\n处理完毕")
    return 0

def main():
    print ("This module should not be run as a stand-alone module")
    return -1
    
if __name__ == "__main__":
    sys.exit(main())
