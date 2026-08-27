#!/bin/bash
# Usage: ./dv-commit.sh "commit message"
MSG="${1:?Usage: dv-commit.sh \"message\"}"

FILES=$(dv status 2>/dev/null | awk '{print $2}' | grep -E "^Assets/_Project|^Assets/Wingman|^Packages|^ProjectSettings" | grep -v "\.unity$")

if [ -z "$FILES" ]; then
  echo "Nothing to commit."
  exit 0
fi

echo "Files to commit:"
echo "$FILES" | sed 's/^/  /'
echo ""
read -p "Commit these? [y/N] " ok
if [ "$ok" = "y" ]; then
  dv commit $FILES -m "$MSG"
else
  echo "Aborted."
fi
