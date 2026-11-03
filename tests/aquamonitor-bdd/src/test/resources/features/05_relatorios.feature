# language: pt
@relatorios
Funcionalidade: Relatórios de impacto hídrico
  Como gestor ESG
  Quero relatórios de consumo consolidados por sensor e por período
  Para comprovar a redução do consumo e prestar contas com transparência

  # ESG (E + G): o relatório classifica o consumo em relação ao limite do sensor
  #   até 80% → Normal | até 100% → Atenção | acima de 100% → Critico

  Contexto:
    Dado que eu estou autenticado como operador

  @positivo @contrato @esg-agua
  Esquema do Cenário: Classificação do consumo do sensor em relação ao limite - <status>
    Dado que existe um sensor "Industrial" cadastrado com limite de 1000 litros
    E o sensor recebe as leituras "<leituras>" em litros
    Quando eu gero o relatório de impacto hídrico do sensor
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "relatorio-sensor"
    E o relatório deve contabilizar 2 leituras
    E o consumo total do relatório deve ser <total> litros
    E o percentual do limite deve ser <percentual>
    E o status de consumo deve ser "<status>"

    Exemplos:
      | leituras | total | percentual | status  |
      | 300, 400 | 700   | 70.0       | Normal  |
      | 500, 400 | 900   | 90.0       | Atenção |
      | 800, 700 | 1500  | 150.0      | Critico |

  @negativo @contrato
  Cenário: Relatório de um sensor inexistente
    Quando eu gero o relatório de impacto hídrico do sensor de identificador 999999
    Então o status da resposta deve ser 404
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Sensor 999999 não encontrado."

  @positivo @contrato @esg-governanca
  Cenário: Relatório geral consolida os sensores ativos e os alertas pendentes
    Dado que existe um sensor "Comercial" cadastrado com limite de 400 litros
    E o sensor recebe as leituras "450" em litros
    Quando eu gero o relatório geral de consumo
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "relatorio-geral"
    E o relatório geral deve incluir o sensor cadastrado
    E o relatório geral deve apontar alertas não resolvidos

  @negativo @contrato
  Cenário: Período com data inicial maior que a final é rejeitado
    Quando eu gero o relatório geral de "2026-12-31" até "2026-01-01"
    Então o status da resposta deve ser 400
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Data início não pode ser maior que data fim."

  @negativo @esg-governanca
  Cenário: Relatórios exigem autenticação
    Quando eu gero o relatório geral sem informar token
    Então o status da resposta deve ser 401
