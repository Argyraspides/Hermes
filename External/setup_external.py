import os, re

old_path="res://"
new_path = ""

curr_package_name = ""

ignore_file_extensions = [".png", ".jpeg", ".jpg", ".ttf", ".otf", ".py"]

for (root, dirs, file_names) in os.walk(os.getcwd()):
    for file_name in file_names:
        if any(extension in file_name for extension in ignore_file_extensions):
            continue

        new_file_content: str = ""
        full_file_path = os.path.join(root, file_name)

        package_name_start_idx = full_file_path.rfind("External/")
        cut_path = full_file_path[package_name_start_idx + len("External") + 1:]
        next_slash_pos = cut_path.find("/")

        if next_slash_pos != -1:
            curr_package_name = cut_path[:next_slash_pos]
            new_path = old_path + "External/" + curr_package_name + "/"

        with open(full_file_path, 'r') as file:
            old_file_content = file.read()
            if old_path not in old_file_content:
                continue
            new_file_content = old_file_content.replace(old_path, new_path)

        with open(full_file_path, 'w') as file:
            file.write(new_file_content)

        if "project.godot" in file_name:
            os.rename(full_file_path, full_file_path.replace("project.godot", "project.godot.bak"))
