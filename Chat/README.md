### Migration
you can use PowerShell.


Add-Migration InitialCreate.


Update-Database.


### Publish

Before installing .NET, you'll need to register the Microsoft key, register the product repository, and install required dependencies. This only needs to be done once per machine.

Open a terminal and run the following commands:

```shell
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
```

Install the .NET SDK
Update the products available for installation, then install the .NET SDK.

In your terminal, run the following commands:

```shell
sudo add-apt-repository universe
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-2.2=2.2.102-1
```

### kakaogame SDK Service API
Resource URL (for Game Server)
  
| Zone | URL |  
| :---: | :---: |  
| QA (Beta) | https://qa-openapi-zinny3.game.kakao.com:10443/service/[version]/[api명] |  
| Real (Live) | https://openapi-zinny3.game.kakao.com:10443/service/[version]/[api명] |  
  
### Jira
https://jira.daumkakao.com/browse/SOS-84