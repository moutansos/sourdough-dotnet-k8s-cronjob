#!/bin/bash

METADATA_FILE="metadata.json"

PROJECT_NAME=$(jq -r '.projectName' $METADATA_FILE)
CONTAINER_NAME=$(jq -r '.containerName' $METADATA_FILE)
TEMPLATE_NAME=$(jq -r '.templateProjectName' $METADATA_FILE)
TEMPLATE_CONTAINER_NAME=$(jq -r '.templateContainerName' $METADATA_FILE)

INFRA_PROJECT_NAME="$PROJECT_NAME.Infra"

function update_directory {
    local dir_name="$1"
    local new_dir_name=$(echo $dir_name | sed -e "s/$TEMPLATE_NAME/$PROJECT_NAME/g" | sed -e "s/$TEMPLATE_CONTAINER_NAME/$CONTAINER_NAME/g")
    if [[ "$new_dir_name" != "$dir_name" ]]; then
        mv "$dir_name" "$new_dir_name"
    fi
}

function update_file {
    local file_name="$1"
    echo "REPLATEMENT: s/$TEMPLATE_NAME/$PROJECT_NAME/g"
    sed -i -e "s/$TEMPLATE_NAME/$PROJECT_NAME/g" $file_name
    sed -i -e "s/<<PROJECT_NAME>>/$PROJECT_NAME/g" $file_name

    echo "REPLATEMENT: s/$TEMPLATE_CONTAINER_NAME/$CONTAINER_NAME/g"
    sed -i -e "s/$TEMPLATE_CONTAINER_NAME/$CONTAINER_NAME/g" $file_name
    sed -i -e "s/<<CONTAINER_NAME>>/$CONTAINER_NAME/g" $file_name

    # Handle replacememts in file name
    local new_file_name=$(echo $file_name | sed -e "s/$TEMPLATE_NAME/$PROJECT_NAME/g" | sed -e "s/$TEMPLATE_CONTAINER_NAME/$CONTAINER_NAME/g")
    if [[ "$new_file_name" != "$file_name" ]]; then
        mv "$file_name" "$new_file_name"
    fi
}

GIT_IGNORED=""

# Recursively find all directories
find . -type d | while read -r dir; do
    GIT_IGNORED=$(git check-ignore $dir)
    if [[ $dir == "." ]]; then
        continue
    elif [[ $GIT_IGNORED != "" ]]; then
        continue
    elif [[ $dir = ./.git/* ]]; then
        continue
    fi

    echo "Processing Directory: $dir"
    update_directory $dir
done

# Recursively find all files
find . -type f | while read -r file; do
    GIT_IGNORED=$(git check-ignore $file)
    if [[ $file == "./setup.sh" ]]; then
        continue
    elif [[ $GIT_IGNORED != "" ]]; then
        continue
    elif [[ $file = ./.git/* ]]; then
        continue
    elif [[ $file = ./metadata.json ]]; then
        continue
    fi

    echo "Processing: $file"
    update_file $file
done

#Setup the README
rm ./README.md
mv ./README.template.md ./README.md

mv ./.github.disabled/ ./.github/
mv ./.gitea.disabled/ ./.gitea/

#Remove Setup File
rm ./setup.sh

#Commit the things
git add .
git commit -m "chore: run setup"

pushd ./$INFRA_PROJECT_NAME/
pulumi stack init dev
popd ..

echo "Setup complete! Please review the changes and push them to your repository."
echo "You need to populate CronJob.Template.Common/Constants.cs with the remaining missing variables."
