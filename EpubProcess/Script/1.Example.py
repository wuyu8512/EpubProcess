import re

# print(epub)
def run(epub):
	for _id in epub.GetTextIDs():
		print(_id)
		content = epub.GetItemContentByID(_id)
		print('\u2022')
		epub.SetItemContentByID(_id, re.sub(r'<p> +', '<p>', content))
