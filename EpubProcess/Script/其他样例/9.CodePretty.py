from lxml import etree

for _id in epub.GetTextIDs():
	print(_id)
	content = epub.GetItemContentByID(_id)
	document_root = etree.HTML(content.encode())
	epub.SetItemContentByID(_id, etree.tostring(document_root, encoding='Unicode', pretty_print=True))