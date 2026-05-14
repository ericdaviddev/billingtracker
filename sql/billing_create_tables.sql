create table billing.clients
(
    client_id   uuid                     default gen_random_uuid() not null
        primary key,
    client_name text                                               not null,
    created_at  timestamp with time zone default now()             not null,
    updated_at  timestamp with time zone default now()             not null
);

alter table billing.clients
    owner to postgres;

create table billing.locations
(
    location_id         uuid                     default gen_random_uuid() not null
        primary key,
    location_name       text                                               not null,
    client_id           uuid                                               not null
        constraint locations_clients_client_id_fk
            references billing.clients,
    managed_by_platform boolean                  default true              not null,
    created_at          timestamp with time zone default now()             not null,
    updated_at          timestamp with time zone default now()             not null
);

alter table billing.locations
    owner to postgres;

create table billing.source_systems
(
    source_system_id   uuid                     default gen_random_uuid() not null
        primary key,
    source_system_name text                                               not null,
    created_at         timestamp with time zone default now()             not null,
    updated_at         timestamp with time zone default now()             not null
);

alter table billing.source_systems
    owner to postgres;

create table billing.client_source_mappings
(
    mapping_id         uuid                     default gen_random_uuid() not null
        primary key,
    client_id          uuid                                               not null
        constraint client_source_mappings_clients_client_id_fk
            references billing.clients,
    source_system_id   uuid                                               not null
        constraint client_source_mappings_source_systems_source_system_id_fk
            references source_systems,
    external_client_id text                                               not null,
    created_at         timestamp with time zone default now()             not null,
    updated_at         timestamp with time zone default now()             not null,
    constraint client_source_mappings_unique_constraint
        unique (client_id, source_system_id, external_client_id)
);

alter table billing.client_source_mappings
    owner to postgres;

create table billing.guarantors
(
    guarantor_id          uuid                     default gen_random_uuid() not null
        constraint guarantors_pk
            primary key,
    first_name            text                                               not null,
    last_name             text                                               not null,
    client_id             uuid                                               not null
        constraint guarantors_clients_client_id_fk
            references clients,
    source_system_id      uuid                                               not null
        constraint guarantors_source_systems_source_system_id_fk
            references source_systems,
    external_guarantor_id text                                               not null,
    email                 text,
    phone                 text,
    source_updated_at     timestamp with time zone                           not null,
    created_at            timestamp with time zone default now()             not null,
    updated_at            timestamp with time zone default now()             not null,
    constraint guarantors_unique_constraint
        unique (client_id, source_system_id, external_guarantor_id)
);

alter table billing.guarantors
    owner to postgres;

create table billing.dependents
(
    dependent_id          uuid                     default gen_random_uuid() not null
        constraint dependents_pk
            primary key,
    client_id             uuid                                               not null
        constraint dependents_clients_client_id_fk
            references clients,
    source_system_id      uuid                                               not null
        constraint dependents_source_systems_source_system_id_fk
            references source_systems,
    guarantor_id          uuid                                               not null
        constraint dependents_guarantors_guarantor_id_fk
            references guarantors,
    external_dependent_id text                                               not null,
    first_name            text                                               not null,
    last_name             text                                               not null,
    date_of_birth         date,
    source_updated_at     timestamp with time zone                           not null,
    created_at            timestamp with time zone default now()             not null,
    updated_at            timestamp with time zone default now()             not null,
    constraint dependents_unique_constraint
        unique (client_id, source_system_id, external_dependent_id)
);

alter table billing.dependents
    owner to postgres;

create table billing.payments
(
    payment_id          uuid                     default gen_random_uuid() not null
        primary key,
    location_id         uuid                                               not null
        constraint payments_locations_location_id_fk
            references locations,
    client_id           uuid                                               not null
        constraint payments_clients_client_id_fk
            references clients,
    payment_amount      numeric(12, 2)           default 0.00              not null,
    payment_date        timestamp with time zone                           not null,
    payment_status      text                                               not null,
    created_at          timestamp with time zone default now()             not null,
    updated_at          timestamp with time zone default now()             not null,
    source_system_id    uuid                                               not null
        constraint payments_source_systems_source_system_id_fk
            references source_systems,
    source_updated_at   timestamp with time zone                           not null,
    guarantor_id        uuid                                               not null
        constraint payments_guarantors_guarantor_id_fk
            references guarantors,
    dependent_id        uuid                                               not null
        constraint payments_dependents_dependent_id_fk
            references dependents,
    external_payment_id text                                               not null,
    constraint payments_unique_constraint
        unique (client_id, source_system_id, external_payment_id)
);

alter table billing.payments
    owner to postgres;

