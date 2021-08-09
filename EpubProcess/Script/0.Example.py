def run(epub):
	print(epub.Title)
	for _id in epub.GetTextIDs():
		print(_id)
		content = epub.GetItemContentByID(_id)
		epub.SetItemContentByID(_id, content)