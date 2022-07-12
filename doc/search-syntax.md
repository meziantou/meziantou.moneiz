# Search transactions syntax

Supported filters:
- `title`
- `payee`
- `category`
- `comment`
- `date`
- `amount`
- `state` (NotChecked, Checked, Reconciled)

Supported operators:
- `:`, `=`, `<>`, `<`, `<=`, `>`, `<=`, `AND`, `OR`, `NOT`

## Example queries:

````
(date>=2022-01-01 AND date<=2022-01-31) OR title:"Transaction title"
````

````
NOT (date>=2022-01-01 AND date<=2022-01-31)"
````

````
amount>=0
````

````
payee<>"sample"
````