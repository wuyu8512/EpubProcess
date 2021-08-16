import re

# 当前py脚本并非调用系统python，而是采用一套Python3.4的.NET实现，故目前不支持C扩展模块，如lxml，pyqt5, 
# 需要调用C模块，请采用替代方案，或者编写C#脚本，bs4 调用 html.parser 可用

def run(epub):
	print(epub.Title)
	for _id in epub.GetTextIDs():
		# print(_id)
		content = epub.GetItemContentByID(_id)
		epub.SetItemContentByID(_id, re.sub(r'<p> +', '<p>', content))