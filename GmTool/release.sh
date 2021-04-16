dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true


chmod 600 ../game_server_key.pem

ssh -i ../game_server_key.pem ec2-user@3.34.1.59 "sudo systemctl stop gmtool.service"
ssh -i ../game_server_key.pem ec2-user@3.34.1.59 "rm -rf gmtool"
ssh -i ../game_server_key.pem ec2-user@3.34.1.59 "sudo rm -rf /var/www/gmtool"
ssh -i ../game_server_key.pem ec2-user@3.34.1.59 "sudo mkdir /var/www/gmtool"
scp -i ../game_server_key.pem -r bin/Release/netcoreapp3.1/linux-x64/publish/ ec2-user@3.34.1.59:~/gmtool
ssh -i ../game_server_key.pem ec2-user@3.34.1.59 "sudo cp -r /home/ec2-user/gmtool/* /var/www/gmtool"
ssh -i ../game_server_key.pem ec2-user@3.34.1.59 "sudo systemctl start gmtool.service"
