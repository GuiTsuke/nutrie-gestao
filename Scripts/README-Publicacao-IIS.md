# Publicação no IIS

Os arquivos executáveis do site ficam em `E:\inetpub\Nutrie\NutrieGestao\App`.

Os dados reais ficam fora da pasta publicada:

- `E:\inetpub\Nutrie\NutrieGestao\Data` — usuários e dados do sistema;
- `E:\inetpub\Nutrie\NutrieGestao\Logs` — logs;
- `E:\inetpub\Nutrie\NutrieGestao\Backups` — cópias de segurança.

Para publicar sem substituir dados de produção, execute na raiz do projeto:

```powershell
.\Scripts\Publicar-IIS.ps1
```

O script só aceita uma pasta de destino chamada `App`. Ele preserva `Data`, `Keys`, `Logs`, `Backups`, `appsettings*.json` e `web.config`.

Não aponte o Publish do Visual Studio diretamente para `E:\inetpub\Nutrie\NutrieGestao`; se for configurar um perfil manual, o destino deve ser `E:\inetpub\Nutrie\NutrieGestao\App`.
