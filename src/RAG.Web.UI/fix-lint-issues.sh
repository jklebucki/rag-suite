#!/bin/bash

# Script to help identify files with lint issues
echo "Files with most issues:"
npm run lint 2>&1 | grep "^/" | sort | uniq -c | sort -rn
