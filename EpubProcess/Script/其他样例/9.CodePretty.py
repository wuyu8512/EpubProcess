import sys
sys.path.append('C:\Program Files\IronPython 3.4\Lib')
sys.path.append('C:\Program Files\IronPython 3.4\Lib\site-packages')
from bs4 import BeautifulSoup

def run(epub):
    for _id in epub.GetTextIDs():
        print(_id)
        # content = epub.GetItemContentByID(_id)
        # doc = BeautifulSoup(content, 'html.parser')
        # epub.SetItemContentByID(_id, doc.prettify())