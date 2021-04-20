dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true


chmod 600 ../game_server_key.pem

ssh -i ../game_server_key.pem ec2-user@13.209.7.77 "sudo systemctl stop login.service"
ssh -i ../game_server_key.pem ec2-user@13.209.7.77 "rm -rf login"
ssh -i ../game_server_key.pem ec2-user@13.209.7.77 "sudo rm -rf /var/www/login"
ssh -i ../game_server_key.pem ec2-user@13.209.7.77 "sudo mkdir /var/www/login"
scp -i ../game_server_key.pem -r bin/Release/netcoreapp3.1/linux-x64/publish/* ec2-user@13.209.7.77:~/login
ssh -i ../game_server_key.pem ec2-user@13.209.7.77 "sudo cp -r /home/ec2-user/login/* /var/www/login"
ssh -i ../game_server_key.pem ec2-user@13.209.7.77 "sudo systemctl start login.service"
