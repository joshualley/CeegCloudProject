# _*_ code: utf-8 _*_
# 出差申请插件汇总

import clr
from System import DateTime

'''
金额转大写 && 汇总表体预估费用至表头总费用 && 验证结束时间大于出差时间
'''
def DataChanged(e):
	# 验证结束时间大于出差时间
	_FDateStart = this.View.BillModel.GetValue('FDateStart', e.Row)
	_FDateEnd = this.View.BillModel.GetValue('FDateEnd', e.Row)
	if e.Key == 'FDateStart':
		if _FDateStart != None and _FDateEnd != None:
			# 开始时间大于结束时间
			if DateTime.Parse(str(_FDateStart)).CompareTo(DateTime.Parse(str(_FDateEnd))) == 1:
				this.View.ShowMessage('出差时间不能在结束时间之后！')
	if e.Key == 'FDateEnd':
		if _FDateStart != None and _FDateEnd != None:
			# 开始时间大于结束时间
			if DateTime.Parse(str(_FDateStart)).CompareTo(DateTime.Parse(str(_FDateEnd))) == 1:
				this.View.ShowMessage('结束时间不能在出差时间之前！')

	# FAmount 为预计金额代理字段标识
	if e.Key == 'FAmount':
		# 预计费用
		PreMoney = float(this.View.BillModel.GetValue('F_ora_Amount'))
		PreMoney = convertNumToChinese(PreMoney)
		AmountDisplay = this.View.BillModel.SetValue('F_ora_AmountDisplay', PreMoney)
	
	# ----------------------------------------------------------------------------------------------------
	if e.Key == 'F_ora_Cost':
		# 表体中预估费用更改时汇总到表头
		count = this.View.BillModel.GetEntryRowCount('FEntity')
		amount = sum([float(this.View.BillModel.GetValue('FExpectCost', row)) for row in range(count)])
		#this.View.ShowMessage(str(amount))
		this.View.BillModel.SetValue('F_ora_Amount', amount)

def convertNumToChinese(totalPrice):
		#import math
		dictChinese = [u'零',u'壹',u'贰',u'叁',u'肆',u'伍',u'陆',u'柒',u'捌',u'玖']
		unitChinese = [u'',u'拾',u'佰',u'仟','',u'拾',u'佰',u'仟']
		#将整数部分和小数部分区分开
		#partA = int(math.floor(totalPrice))
		partA = int(totalPrice)
		partB = round(totalPrice-partA, 2)
		strPartA = str(partA)
		strPartB = ''
		if partB != 0:
			strPartB = str(partB)[2:]

		singleNum = []
		if len(strPartA) != 0:
			i = 0
			while i < len(strPartA):
				singleNum.append(strPartA[i])
				i = i+1
		#将整数部分先压再出，因为可以从后向前处理，好判断位数 
		tnumChinesePartA = []
		numChinesePartA = []
		j = 0
		bef = '0';
		if len(strPartA) != 0:
			while j < len(strPartA) :
				curr = singleNum.pop()
				if curr == '0' and bef !='0':
					tnumChinesePartA.append(dictChinese[0])
					bef = curr
				if curr != '0':
					tnumChinesePartA.append(unitChinese[j])
					tnumChinesePartA.append(dictChinese[int(curr)])
					bef = curr
				if j == 3:
					tnumChinesePartA.append(u'萬')
					bef = '0'
				j = j+1

			for i in range(len(tnumChinesePartA)):
				numChinesePartA.append(tnumChinesePartA.pop())
		A = ''      
		for i in numChinesePartA:
			A = A+i
		#小数部分很简单，只要判断下角是否为零
		B = ''
		if len(strPartB) == 1:
			B = dictChinese[int(strPartB[0])] + u'角'
		if len(strPartB) == 2 and strPartB[0] != '0':
			B = dictChinese[int(strPartB[0])] + u'角' + dictChinese[int(strPartB[1])] + u'分'
		if len(strPartB) == 2 and strPartB[0] == '0':
			B = dictChinese[int(strPartB[0])] + dictChinese[int(strPartB[1])] + u'分'

		if len(strPartB) == 0:
			S = A + u'元'
		if len(strPartB)!= 0:
			S = A + u'元' +B
		return S

	
