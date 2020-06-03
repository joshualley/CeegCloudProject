
''' 出差报销
合计补贴金额
'''
def DataChanged(e):
    if e.Key in ('FCarAmount', 'FType'): # 金额
        row = this.View.Model.GetEntryRowCount('FEntity')
        subTAmount = 0
        for row_i in range(row):
            ftype = this.View.Model.GetValue('FType', row_i)
            if ftype == '2':
                subAmount = this.View.Model.GetValue('FCarAmount', row_i)

                subTAmount += float(subAmount)

        this.View.Model.SetValue('FSubsidyAmount', subTAmount)