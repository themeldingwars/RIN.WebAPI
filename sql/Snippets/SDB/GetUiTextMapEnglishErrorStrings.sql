select key, english from sdb."dblocalization::LocalizedText" join sdb."dblocalization::UITextMap" on "dblocalization::UITextMap".id = sdb."dblocalization::LocalizedText".id
where key like 'ERR%'