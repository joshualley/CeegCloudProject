# 招待费用申请PlugIn

'''
金额转大写
'''
def DataChanged(e):
	# FAmount 为预计金额代理字段标识
	if e.Key == 'FAmount':
		# 预计费用
		PreMoney = float(this.View.BillModel.GetValue('F_ora_Amount'))
		PreMoney = convertNumToChinese(PreMoney)
		AmountDisplay = this.View.BillModel.SetValue('F_ora_AmountDisplay', PreMoney)



# 金额转大写工具函数
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