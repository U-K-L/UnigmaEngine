
n = 100000000
filename = "BasicNN.csv"
with open(filename, 'w') as file:
    for i in range(0, n ):
        if i < n -1:
            file.write("1,")
        else:
            file.write("1")
