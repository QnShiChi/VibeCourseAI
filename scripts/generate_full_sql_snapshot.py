#!/usr/bin/env python3
from __future__ import annotations

import argparse
import datetime as dt
import decimal
import uuid
from collections import defaultdict
from pathlib import Path

import pymssql


STRING_TYPES = {
    "char",
    "nchar",
    "varchar",
    "nvarchar",
    "text",
    "ntext",
}


def sql_literal(value):
    if value is None:
        return "NULL"
    if isinstance(value, bool):
        return "1" if value else "0"
    if isinstance(value, uuid.UUID):
        return f"'{value}'"
    if isinstance(value, (int, decimal.Decimal)):
        return str(value)
    if isinstance(value, float):
        if value != value:
            return "NULL"
        return repr(value)
    if isinstance(value, dt.datetime):
        return f"'{value.isoformat(sep='T', timespec='microseconds')}'"
    if isinstance(value, dt.date):
        return f"'{value.isoformat()}'"
    if isinstance(value, bytes):
        return "0x" + value.hex()
    text = str(value).replace("'", "''")
    return f"N'{text}'"


def column_type(column):
    type_name = column["type_name"]
    max_length = column["max_length"]
    precision = column["precision"]
    scale = column["scale"]

    if type_name in {"nvarchar", "nchar"}:
        return f"{type_name}(max)" if max_length == -1 else f"{type_name}({max_length // 2})"
    if type_name in {"varchar", "char", "varbinary", "binary"}:
        return f"{type_name}(max)" if max_length == -1 else f"{type_name}({max_length})"
    if type_name in {"decimal", "numeric"}:
        return f"{type_name}({precision},{scale})"
    if type_name in {"datetime2", "datetimeoffset", "time"}:
        return f"{type_name}({scale})"
    if type_name == "float":
        return "float"
    return type_name


def fetch_all(cursor, query):
    cursor.execute(query)
    return cursor.fetchall()


def identity_int(value):
    if isinstance(value, (bytes, bytearray)):
        return int.from_bytes(value, byteorder="little", signed=True)
    return int(value)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--server", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=1434)
    parser.add_argument("--user", default="sa")
    parser.add_argument("--password", required=True)
    parser.add_argument("--database", default="vibe_course_ai_db")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()

    conn = pymssql.connect(
        server=args.server,
        port=args.port,
        user=args.user,
        password=args.password,
        database=args.database,
        charset="UTF-8",
        as_dict=True,
    )
    cursor = conn.cursor(as_dict=True)

    tables = fetch_all(
        cursor,
        """
        SELECT s.name AS schema_name, t.name AS table_name, t.object_id
        FROM sys.tables t
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.is_ms_shipped = 0
        ORDER BY s.name, t.name
        """,
    )

    columns = fetch_all(
        cursor,
        """
        SELECT
            t.name AS table_name,
            c.column_id,
            c.name AS column_name,
            ty.name AS type_name,
            c.max_length,
            c.precision,
            c.scale,
            c.is_nullable,
            c.is_identity,
            ic.seed_value,
            ic.increment_value,
            dc.name AS default_name,
            dc.definition AS default_definition
        FROM sys.tables t
        JOIN sys.columns c ON c.object_id = t.object_id
        JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
        LEFT JOIN sys.identity_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        WHERE t.is_ms_shipped = 0
        ORDER BY t.name, c.column_id
        """,
    )

    pk_rows = fetch_all(
        cursor,
        """
        SELECT
            t.name AS table_name,
            kc.name AS constraint_name,
            c.name AS column_name,
            ic.key_ordinal,
            ic.is_descending_key
        FROM sys.key_constraints kc
        JOIN sys.tables t ON t.object_id = kc.parent_object_id
        JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE kc.type = 'PK'
        ORDER BY t.name, ic.key_ordinal
        """,
    )

    fk_rows = fetch_all(
        cursor,
        """
        SELECT
            fk.name AS fk_name,
            pt.name AS parent_table,
            pc.name AS parent_column,
            rt.name AS referenced_table,
            rc.name AS referenced_column,
            fkc.constraint_column_id,
            fk.delete_referential_action_desc AS delete_action,
            fk.update_referential_action_desc AS update_action
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
        JOIN sys.columns pc ON pc.object_id = pt.object_id AND pc.column_id = fkc.parent_column_id
        JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
        JOIN sys.columns rc ON rc.object_id = rt.object_id AND rc.column_id = fkc.referenced_column_id
        ORDER BY fk.name, fkc.constraint_column_id
        """,
    )

    index_rows = fetch_all(
        cursor,
        """
        SELECT
            t.name AS table_name,
            i.name AS index_name,
            i.is_unique,
            i.has_filter,
            i.filter_definition,
            ic.is_included_column,
            ic.key_ordinal,
            ic.index_column_id,
            ic.is_descending_key,
            c.name AS column_name
        FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE t.is_ms_shipped = 0
          AND i.name IS NOT NULL
          AND i.is_primary_key = 0
          AND i.is_unique_constraint = 0
          AND i.type_desc <> 'HEAP'
        ORDER BY t.name, i.name, ic.is_included_column, ic.key_ordinal, ic.index_column_id
        """,
    )

    check_rows = fetch_all(
        cursor,
        """
        SELECT t.name AS table_name, cc.name AS constraint_name, cc.definition
        FROM sys.check_constraints cc
        JOIN sys.tables t ON t.object_id = cc.parent_object_id
        ORDER BY t.name, cc.name
        """,
    )

    migrations = fetch_all(cursor, "SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory")

    cols_by_table = defaultdict(list)
    for row in columns:
        cols_by_table[row["table_name"]].append(row)

    pk_by_table = defaultdict(list)
    pk_name_by_table = {}
    for row in pk_rows:
        pk_by_table[row["table_name"]].append(row)
        pk_name_by_table[row["table_name"]] = row["constraint_name"]

    fks_by_name = defaultdict(list)
    for row in fk_rows:
        fks_by_name[row["fk_name"]].append(row)

    idx_by_name = defaultdict(list)
    for row in index_rows:
        idx_by_name[(row["table_name"], row["index_name"])].append(row)

    checks_by_table = defaultdict(list)
    for row in check_rows:
        checks_by_table[row["table_name"]].append(row)

    out = []
    out.append(f"-- Full SQL Server snapshot for [{args.database}]")
    out.append(f"-- Generated at {dt.datetime.now(dt.timezone.utc).isoformat()}")
    out.append("USE master;")
    out.append("GO\n")
    out.append(f"IF DB_ID(N'{args.database}') IS NOT NULL")
    out.append("BEGIN")
    out.append(f"    ALTER DATABASE [{args.database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;")
    out.append(f"    DROP DATABASE [{args.database}];")
    out.append("END")
    out.append("GO\n")
    out.append(f"CREATE DATABASE [{args.database}];")
    out.append("GO\n")
    out.append(f"USE [{args.database}];")
    out.append("GO\n")

    # Tables with PK and defaults inline, but no foreign keys.
    for table in tables:
        table_name = table["table_name"]
        out.append(f"CREATE TABLE [dbo].[{table_name}] (")
        lines = []
        for column in cols_by_table[table_name]:
            line = f"    [{column['column_name']}] {column_type(column)}"
            if column["is_identity"]:
                seed = identity_int(column["seed_value"])
                incr = identity_int(column["increment_value"])
                line += f" IDENTITY({seed},{incr})"
            if column["default_definition"]:
                line += f" CONSTRAINT [{column['default_name']}] DEFAULT {column['default_definition']}"
            line += " NULL" if column["is_nullable"] else " NOT NULL"
            lines.append(line)

        if pk_by_table.get(table_name):
            cols = []
            for pk in pk_by_table[table_name]:
                direction = "DESC" if pk["is_descending_key"] else "ASC"
                cols.append(f"[{pk['column_name']}] {direction}")
            lines.append(f"    CONSTRAINT [{pk_name_by_table[table_name]}] PRIMARY KEY ({', '.join(cols)})")

        out.append(",\n".join(lines))
        out.append(");")
        out.append("GO\n")

    # Data inserts.
    for table in tables:
        table_name = table["table_name"]
        table_columns = cols_by_table[table_name]
        column_names = [row["column_name"] for row in table_columns]
        identity_cols = [row["column_name"] for row in table_columns if row["is_identity"]]

        order_by = ""
        if pk_by_table.get(table_name):
            pk_order = ", ".join(f"[{row['column_name']}]" for row in pk_by_table[table_name])
            order_by = f" ORDER BY {pk_order}"

        data_cursor = conn.cursor(as_dict=True)
        data_cursor.execute(f"SELECT * FROM [dbo].[{table_name}]{order_by}")
        rows = data_cursor.fetchall()

        if not rows:
            continue

        if identity_cols:
            out.append(f"SET IDENTITY_INSERT [dbo].[{table_name}] ON;")
            out.append("GO")

        batch_lines = []
        for row in rows:
            values = ", ".join(sql_literal(row[col]) for col in column_names)
            cols = ", ".join(f"[{col}]" for col in column_names)
            batch_lines.append(f"INSERT INTO [dbo].[{table_name}] ({cols}) VALUES ({values});")

        out.extend(batch_lines)
        out.append("GO")

        if identity_cols:
            out.append(f"SET IDENTITY_INSERT [dbo].[{table_name}] OFF;")
            out.append("GO")
        out.append("")

    # Check constraints.
    for table_name, checks in checks_by_table.items():
        for check in checks:
            out.append(
                f"ALTER TABLE [dbo].[{table_name}] ADD CONSTRAINT [{check['constraint_name']}] CHECK {check['definition']};"
            )
        out.append("GO\n")

    # Non-PK indexes.
    for (table_name, index_name), rows in idx_by_name.items():
        key_cols = []
        include_cols = []
        is_unique = bool(rows[0]["is_unique"])
        filter_definition = rows[0]["filter_definition"]

        for row in rows:
            if row["is_included_column"]:
                include_cols.append(f"[{row['column_name']}]")
            else:
                direction = "DESC" if row["is_descending_key"] else "ASC"
                key_cols.append(f"[{row['column_name']}] {direction}")

        statement = f"CREATE {'UNIQUE ' if is_unique else ''}INDEX [{index_name}] ON [dbo].[{table_name}] ({', '.join(key_cols)})"
        if include_cols:
            statement += f" INCLUDE ({', '.join(include_cols)})"
        if filter_definition:
            statement += f" WHERE {filter_definition}"
        statement += ";"
        out.append(statement)
    out.append("GO\n")

    # Foreign keys after data load.
    for fk_name, rows in fks_by_name.items():
        parent_table = rows[0]["parent_table"]
        referenced_table = rows[0]["referenced_table"]
        parent_columns = ", ".join(f"[{row['parent_column']}]" for row in rows)
        referenced_columns = ", ".join(f"[{row['referenced_column']}]" for row in rows)
        statement = (
            f"ALTER TABLE [dbo].[{parent_table}] WITH CHECK ADD CONSTRAINT [{fk_name}] "
            f"FOREIGN KEY ({parent_columns}) REFERENCES [dbo].[{referenced_table}] ({referenced_columns})"
        )
        if rows[0]["delete_action"] != "NO_ACTION":
            statement += f" ON DELETE {rows[0]['delete_action'].replace('_', ' ')}"
        if rows[0]["update_action"] != "NO_ACTION":
            statement += f" ON UPDATE {rows[0]['update_action'].replace('_', ' ')}"
        statement += ";"
        out.append(statement)
        out.append(f"ALTER TABLE [dbo].[{parent_table}] CHECK CONSTRAINT [{fk_name}];")
    out.append("GO\n")

    output_path = Path(args.output)
    output_path.write_text("\n".join(out), encoding="utf-8")
    conn.close()
    print(f"Wrote {output_path}")
    print(f"Tables: {len(tables)}")
    print(f"Migrations rows: {len(migrations)}")


if __name__ == "__main__":
    main()
