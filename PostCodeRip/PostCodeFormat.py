import re
import json

Areas = []


def extract():
    rg = re.compile(
        '^([a-z ]*?)(?:\s)?\n?((?:[a-z]{2,3}))(?:\s)?(\d{4})$', re.IGNORECASE | re.MULTILINE)

    with open("postCodes.txt", 'r') as f, open("postCodesFixed.txt", 'w') as out:
        contents = f.read()
        for match in rg.findall(contents):
            out.write("{}\n".format(' '.join(match)))
            if match[1] == "VIC":
                a1 = {"Suburb": match[0], "State": match[1], "Zip": match[2]}
                Areas.append(a1)

        out.close()


extract()

with open(r"..\Data\postalAreas.json", 'w') as out:
    out.write(json.dumps(Areas, indent=4))
