COMPOSE := docker compose
DB_NAME := vibe_course_ai_db
DB_USER := sa
DB_PASSWORD := VibeCourse@123
BACKUP_DIR := backups
TIMESTAMP := $(shell date +%Y%m%d_%H%M%S)
BACKUP_BAK := $(DB_NAME)_$(TIMESTAMP).bak
BACKUP_SQL := $(DB_NAME)_$(TIMESTAMP).sql
SQLCMD := /opt/mssql-tools18/bin/sqlcmd -S localhost -U $(DB_USER) -P "$(DB_PASSWORD)" -C

.PHONY: up down ps logs backup-db restore

up:
	$(COMPOSE) up -d

down:
	$(COMPOSE) down --remove-orphans

ps:
	$(COMPOSE) ps

logs:
	$(COMPOSE) logs -f

backup-db:
	mkdir -p $(BACKUP_DIR)
	$(COMPOSE) exec -T sqlserver mkdir -p /var/opt/mssql/backup
	$(COMPOSE) exec -T sqlserver $(SQLCMD) -Q "BACKUP DATABASE [$(DB_NAME)] TO DISK = N'/var/opt/mssql/backup/$(BACKUP_BAK)' WITH INIT, COPY_ONLY"
	$(COMPOSE) cp sqlserver:/var/opt/mssql/backup/$(BACKUP_BAK) $(BACKUP_DIR)/$(BACKUP_BAK)
	printf '%s\n' \
		"-- Generated restore helper for $(DB_NAME)" \
		"-- Preferred full restore artifact: $(BACKUP_BAK)" \
		"-- Use: make restore FILE=$(BACKUP_DIR)/$(BACKUP_BAK)" \
		"SELECT '$(DB_NAME)' AS database_name, '$(BACKUP_BAK)' AS backup_file;" \
		> $(BACKUP_DIR)/$(BACKUP_SQL)

restore:
	test -n "$(FILE)" || (echo "Usage: make restore FILE=$(BACKUP_DIR)/<file.sql|file.bak>"; exit 1)
	case "$(FILE)" in \
	  *.bak) \
	    cp "$(FILE)" /tmp/restore.bak && \
	    $(COMPOSE) exec -T sqlserver mkdir -p /var/opt/mssql/backup && \
	    $(COMPOSE) cp /tmp/restore.bak sqlserver:/var/opt/mssql/backup/restore.bak && \
	    $(COMPOSE) exec -T sqlserver $(SQLCMD) -Q "USE master; IF DB_ID('$(DB_NAME)') IS NOT NULL BEGIN ALTER DATABASE [$(DB_NAME)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$(DB_NAME)]; END; RESTORE DATABASE [$(DB_NAME)] FROM DISK = N'/var/opt/mssql/backup/restore.bak' WITH MOVE '$(DB_NAME)' TO '/var/opt/mssql/data/$(DB_NAME).mdf', MOVE '$(DB_NAME)_log' TO '/var/opt/mssql/data/$(DB_NAME)_log.ldf', REPLACE";; \
	  *.sql) \
	    $(COMPOSE) exec -T sqlserver $(SQLCMD) -d $(DB_NAME) -i /dev/stdin < "$(FILE)";; \
	  *) \
	    echo "Unsupported restore file: $(FILE)"; exit 1;; \
	esac
