originalFolder="$1"
targetFolder="$2"
executablePath="$3"
cpResult=1
while [ $cpResult -ne 0 ]; do
  sleep 1
  cp -r "$originalFolder"/* "$targetFolder" 2> error.log
  echo "cp $originalFolder to $targetFolder"
  cpResult=$?
done
echo "remove $originalFolder"
rm -r "$originalFolder"
echo "ready for start"
nohup "$executablePath" >/dev/null 2>&1 &
sleep 10
echo "ready for exit"
exit
