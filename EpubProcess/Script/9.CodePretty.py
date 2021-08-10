import sys
print(-1)
sys.path.append('C:/Python39/Lib/site-packages/')
print(4)
sys.path.append('C:/Python39/Lib/')
print(5)
sys.path.append('C:/Python39/Lib/site-packages/lxml')
print(6)

from lxml import etree, html

def run(epub):
	print(0)

	for _id in epub.GetTextIDs():
		print(1)
		content = epub.GetItemContentByID(_id)
		print(2)
		document_root = html.fromstring(content)
		print(3)
		epub.SetItemContentByID(_id, etree.tostring(document_root, encoding='unicode', pretty_print=True))