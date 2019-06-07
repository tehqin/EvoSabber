import numpy as np
import seaborn as sns
import matplotlib.pyplot as plt

width = 8
numLines = 10
filepath = 'sliding-ex.png'

f, axes = plt.subplots(1, 2, figsize=(8, 4), sharex=True)

sns.set(style="white", palette="muted", color_codes=True)
rs = np.random.RandomState(10)
d = list(rs.normal(loc=2.0, size=10 ** 3)) + list(rs.normal(loc=5.0, size=10 ** 3))

# Draw plot 1 with uniform lines accross a value
p1 = sns.distplot(d, hist=False, color="g", kde_kws={"shade": True}, ax=axes[0])
for i in range(numLines):
    x = i * width / (numLines-1)
    print(x)
    p1.plot([x, x], [0, 0.25], linestyle='--', color='b', linewidth=2)

# Draw plot 1 with distribution lines
vs = sorted(d)
p2 = sns.distplot(d, hist=False, color="g", kde_kws={"shade": True}, ax=axes[1])
for i in range(numLines):
    index = i*(len(vs)-1) // (numLines-1)
    print(index)
    x = vs[index]
    p2.plot([x, x], [0, 0.25], linestyle='--', color='b', linewidth=2)

sns.despine(left=True, right=True, top=True)

plt.setp(axes, yticks=[])
plt.tight_layout()

plt.savefig(filepath)

plt.close('all')
