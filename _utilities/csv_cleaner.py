CSV = "./assets/financien.csv"

# log the content of the file
content = ""
with open(CSV, "r") as f:
    content = f.read()

# Remove spaces and questionmarks, and log the result
cleaned_content = content.replace(" ", "").replace("?", "").replace("-", "")
print(cleaned_content)

# Write the result to a new file
with open("./assets/financien_cleaned.csv", "w") as f:
    f.write(cleaned_content)