#!/usr/bin/env python3
import pandas as pd
import os


def open_csv(path=""):
    if not os.path.isfile(path):
        print("No file named {}".format(path))
        exit()
    # with open(path) as file:
    #     raw_csv_lines = file.readlines()
    # return raw_csv_lines
    return pd.read_csv(path)


if __name__ == "__main__":
    print(open_csv("braco.R.csv"))
