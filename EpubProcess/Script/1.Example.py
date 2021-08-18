import re

print(epub)
for _id in epub.GetTextIDs():
	print(_id)
	content = epub.GetItemContentByID(_id)
	epub.SetItemContentByID(_id, re.sub(r'<p> +', '<p>', content))
