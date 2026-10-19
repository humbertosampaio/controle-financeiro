# Só existe para o Dependabot observar a imagem do Postgres (ecossistema docker). Ao mudar a tag,
# atualize também PostgresImagem.cs: um teste de integração confere que os dois batem.
FROM docker.io/library/postgres:18.3
