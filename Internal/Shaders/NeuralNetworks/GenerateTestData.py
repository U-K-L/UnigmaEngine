
n = 2500000
filename = "../../../../StreamingAssets/NeuralNetworks/Data/VectorC.csv"
with open(filename, 'w') as file:
    for i in range(1, n+1 ):
        if i < n:
            file.write(str(i)+",")
        else:
            file.write(str(i))
