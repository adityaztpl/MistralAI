create table app_users (
    id uniqueidentifier not null constraint df_app_users_id default newsequentialid(),
    email nvarchar(320) not null,
    password_hash nvarchar(255) not null,
    display_name nvarchar(120) not null,
    role nvarchar(30) not null constraint df_app_users_role default 'USER',
    enabled bit not null constraint df_app_users_enabled default 1,
    created_at datetime2 not null constraint df_app_users_created_at default sysutcdatetime(),
    updated_at datetime2 not null constraint df_app_users_updated_at default sysutcdatetime(),
    constraint pk_app_users primary key (id),
    constraint uq_app_users_email unique (email),
    constraint ck_app_users_role check (role in ('USER', 'ADMIN'))
);

create table categories (
    id uniqueidentifier not null constraint df_categories_id default newsequentialid(),
    name nvarchar(80) not null,
    slug nvarchar(100) not null,
    created_at datetime2 not null constraint df_categories_created_at default sysutcdatetime(),
    constraint pk_categories primary key (id),
    constraint uq_categories_slug unique (slug)
);

create table products (
    id uniqueidentifier not null constraint df_products_id default newsequentialid(),
    owner_id uniqueidentifier not null,
    category_id uniqueidentifier null,
    sku nvarchar(40) not null,
    name nvarchar(80) not null,
    description nvarchar(max) null,
    price decimal(12, 2) not null,
    currency char(3) not null,
    quantity int not null,
    status nvarchar(20) not null constraint df_products_status default 'ACTIVE',
    version bigint not null constraint df_products_version default 0,
    created_at datetime2 not null constraint df_products_created_at default sysutcdatetime(),
    updated_at datetime2 not null constraint df_products_updated_at default sysutcdatetime(),
    constraint pk_products primary key (id),
    constraint fk_products_owner foreign key (owner_id) references app_users(id),
    constraint fk_products_category foreign key (category_id) references categories(id),
    constraint uq_products_owner_sku unique (owner_id, sku),
    constraint ck_products_price_non_negative check (price >= 0),
    constraint ck_products_quantity_non_negative check (quantity >= 0),
    constraint ck_products_status check (status in ('DRAFT', 'ACTIVE', 'ARCHIVED', 'DELETED'))
);

create table product_audit_events (
    id uniqueidentifier not null constraint df_product_audit_events_id default newsequentialid(),
    product_id uniqueidentifier not null,
    actor_user_id uniqueidentifier null,
    event_type nvarchar(60) not null,
    details_json nvarchar(max) not null constraint df_product_audit_events_details default '{}',
    created_at datetime2 not null constraint df_product_audit_events_created_at default sysutcdatetime(),
    constraint pk_product_audit_events primary key (id),
    constraint fk_product_audit_events_product foreign key (product_id) references products(id),
    constraint fk_product_audit_events_actor foreign key (actor_user_id) references app_users(id),
    constraint ck_product_audit_events_details_json check (isjson(details_json) = 1)
);

create index ix_products_owner_status_created
    on products (owner_id, status, created_at desc);

create index ix_products_owner_category_status_name
    on products (owner_id, category_id, status, name);

create index ix_products_active_owner_created
    on products (owner_id, created_at desc)
    where status = 'ACTIVE';

create index ix_product_audit_product_created
    on product_audit_events (product_id, created_at desc);

insert into categories (name, slug)
select 'Apparel', 'apparel'
where not exists (select 1 from categories where slug = 'apparel');

insert into categories (name, slug)
select 'Electronics', 'electronics'
where not exists (select 1 from categories where slug = 'electronics');

insert into categories (name, slug)
select 'Home', 'home'
where not exists (select 1 from categories where slug = 'home');

insert into categories (name, slug)
select 'Outdoor', 'outdoor'
where not exists (select 1 from categories where slug = 'outdoor');
