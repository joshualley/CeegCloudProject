

persons = []
def DataChanged(e):
	if e.Key == 'FMeetingPeople':
		global persons
		if e.NewValue is not None:
			persons = e.OldValue
			for p in e.NewValue:
				persons.append(p)
			this.View.BillModel.SetValue('FMeetingPeople', persons)



