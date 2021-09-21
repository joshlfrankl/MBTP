import os
from shutil import copyfile
import natsort

def gather_all_files(input_dir):
    paths = []
    for folder in os.scandir("{}".format(input_dir)):
        if folder.is_dir():
            for file in os.scandir(folder.path):
                if file.is_file():
                    paths.append(file.path)
    return(paths)

def save_ordered_paths(paths, out_dir):
    sorted_paths = natsort.natsorted(paths)
    
    for i, p in enumerate(sorted_paths):
        copyfile(p, "{}/{}.png".format(out_dir, i))

if __name__ == "__main__":
    files = gather_all_files("/tmp/image_dump")
    save_ordered_paths(files)

