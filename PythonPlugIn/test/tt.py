import clr

from System import DateTime, Guid

now = DateTime.Now.Day
print(now)



print(Guid.NewGuid())


date = "2020-05-31 00:00:00.000"
x = DateTime.Parse(date)

print(x)