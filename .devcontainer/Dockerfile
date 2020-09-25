FROM mcr.microsoft.com/dotnet/sdk:5.0

# install 3.1 sdk as it's needed by some of the vscode tooling
RUN wget https://packages.microsoft.com/config/ubuntu/19.10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
  dpkg -i packages-microsoft-prod.deb && \
  apt-get update && \
  apt-get install -y apt-transport-https gnupg && \
  apt-get update && \
  apt-get install -y dotnet-sdk-3.1

# install k6 load testing tool
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 379CE192D401AB61 && \
  echo "deb https://dl.bintray.com/loadimpact/deb stable main" | tee -a /etc/apt/sources.list && \
  apt-get update && \
  apt-get install k6