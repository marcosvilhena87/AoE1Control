# Testes previstos

O pacote não inclui um projeto de testes automatizados porque o ambiente principal depende de um processo real `EMPIRES.EXE`.

Critérios manuais mínimos:

1. Com o jogo fechado, deve ocorrer `GameProcessNotFoundException`.
2. Com a versão correta aberta, a conexão deve ser concluída.
3. No menu, `TryGetSnapshot` deve retornar `false`.
4. Dentro de uma partida, deve retornar os quatro recursos.
5. Gastar ou receber recursos deve atualizar os valores.
6. Reiniciar o cenário deve fazer a cadeia ser resolvida novamente.
7. Fechar o jogo deve encerrar o loop.
8. A biblioteca não deve solicitar permissão de escrita.
