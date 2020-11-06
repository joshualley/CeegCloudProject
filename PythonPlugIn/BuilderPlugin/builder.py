"""
显示表体过滤
"""


def CreateControl(e):
	if e.ControlAppearance.OriginKey in ("FEntity"):
		e.Control.Put("showFilterRow", True)

