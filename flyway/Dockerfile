FROM flyway/flyway:10-alpine

COPY conf/flyway.conf /flyway/conf/flyway.conf
COPY sql/ /flyway/sql/

ENTRYPOINT ["flyway"]
CMD ["migrate"]
