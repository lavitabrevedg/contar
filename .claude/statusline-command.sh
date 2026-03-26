#!/usr/bin/env bash
input=$(cat)

cwd=$(echo "$input" | jq -r '.cwd // .workspace.current_dir // empty')
model=$(echo "$input" | jq -r '.model.display_name // empty')
used=$(echo "$input" | jq -r '.context_window.used_percentage // empty')

# Get git branch (skip optional locks to avoid blocking)
branch=""
if [ -n "$cwd" ]; then
  branch=$(git -C "$cwd" -c gc.auto=0 symbolic-ref --short HEAD 2>/dev/null)
fi

# Build status line parts
parts=()

[ -n "$cwd" ] && parts+=("$(printf '\033[0;34m%s\033[0m' "$cwd")")
[ -n "$branch" ] && parts+=("$(printf '\033[0;35m[%s]\033[0m' "$branch")")
[ -n "$model" ] && parts+=("$(printf '\033[0;36m%s\033[0m' "$model")")
if [ -n "$used" ]; then
  used_int=$(printf '%.0f' "$used")
  if [ "$used_int" -ge 80 ]; then
    color='\033[0;31m'
  elif [ "$used_int" -ge 50 ]; then
    color='\033[0;33m'
  else
    color='\033[0;32m'
  fi
  parts+=("$(printf "${color}ctx:%s%%\033[0m" "$used_int")")
fi

# Join parts with separator
printf '%s' "${parts[0]}"
for part in "${parts[@]:1}"; do
  printf ' | %s' "$part"
done
printf '\n'
