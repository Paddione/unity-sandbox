# Cinderreach dedicated server (cinderreach-dedicated-server S3).
# Context: the Linux Server build (SandboxBuilder.BuildDiabloZoneLinuxServer) plus this file,
# staged by scripts/publish-server-image.sh.
FROM ubuntu:24.04
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --chown=1000:1000 server/ /app/
# ubuntu:24.04 ships user "ubuntu" with UID 1000.
USER 1000
ENV HOME=/home/ubuntu
# Relay + Lobby publish, log to stdout, ready file for the probes.
ENTRYPOINT ["/app/cinderreach-server", "-batchmode", "-nographics", "-server", "-server-relay", \
            "-ready-file", "/tmp/cinderreach-ready", "-logFile", "-"]
