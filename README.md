# server

[![standard-readme compliant](https://img.shields.io/badge/standard--readme-OK-green.svg?style=flat-square)](https://github.com/RichardLitt/standard-readme)

[bazzi.gg](https://bazzi.gg)의 API Server입니다.  
ASP.NET Core(.NET 6)로 개발되었습니다.

## Table of Contents

- [Install](#install)
- [Usage](#usage)
- [Maintainers](#maintainers)
- [Contributing](#contributing)
- [Development](#development)
- [License](#license)

## Install

```
docker pull ghcr.io/bazzi-gg/server:latest
```

## Usage

```
docker container run -d -p 80:80 \
-e APP_ConnectionStrings__App=server=localhost;port=3306;database=App;uid=root;password=test \
-e APP_JwtOptions__Secret= \
-e APP_JwtOptions__Key= \
-e APP_KartriderApiKey= \
-e APP_Sentry__Dsn= \
--name server ghcr.io/bazzi-gg/server:latest
```

## Maintainers

[@mschadev](https://github.com/mschadev)

## Contributing

[기여 하시기 전 참고 사항](./CONTRIBUTING.md)

## Development

1. `Server/appsettings.example.json`에서 공백인 **value**값을 채웁니다.
2. 파일명을 `Server/appsettings.example.json`에서 `Server/appsettings.development.json`으로 변경합니다.
3. Server.sln을 Rider(Jetbrains) 혹은 Visual Studio로 엽니다.

## Versioning

[ZeroVer](https://0ver.org/)를 사용합니다.

- API 추가, 삭제, 혹은 기존 API와 호환되지 않을 때: `0.UP.*`
- 그외: `0.*.UP`

## License

[MIT](./LICENSE)
