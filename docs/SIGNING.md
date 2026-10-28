# Assinatura do instalador

A versão 0.14.0 permanece **sem assinatura digital pública**. O responsável pelo projeto ainda não possui certificado ou serviço de assinatura verificado. O SHA256 publicado verifica a integridade do download, mas não substitui a assinatura de identidade.

O fluxo está preparado em `scripts/Sign-Release.ps1`. A consulta abaixo não altera arquivos nem o armazenamento de certificados:

```powershell
./scripts/Sign-Release.ps1 -CheckOnly
```

Quando houver um certificado legítimo de assinatura de código disponível em `CurrentUser/My`, com chave privada acessível, use um SignTool já instalado e o endpoint RFC 3161 fornecido pela autoridade certificadora:

```powershell
./scripts/Sign-Release.ps1 -SignTool 'CAMINHO/signtool.exe' -Thumbprint 'THUMBPRINT_DE_40_CARACTERES' -TimestampUrl 'https://ENDPOINT-DA-AUTORIDADE'
```

O script assina uma cópia em uma pasta nova de `artifacts`, verifica assinatura, identidade e timestamp e gera um novo SHA256. Só publique essa cópia após a verificação. Ele não instala certificados, não solicita senhas no código e não altera configurações do Windows. O instalador original é preservado. Não coloque chaves privadas ou credenciais no repositório.

Foi validada a consulta e a recusa de parâmetros incompletos. A assinatura real e sua cadeia de confiança ficam pendentes até existir um certificado. Assinatura não constitui garantia de ausência de avisos do SmartScreen.

Referência: [Microsoft SignTool](https://learn.microsoft.com/en-us/windows/desktop/seccrypto/signtool), SHA256 para arquivo e timestamp e verificação pela política Authenticode.

A partir da 0.14, o motor é Inno Setup. O script atual assina somente o executável externo. Para uma distribuição integralmente assinada, configurar também SignTool e SignedUninstaller no projeto Inno antes de compilar, usando o certificado legítimo. Essa etapa permanece pendente.
