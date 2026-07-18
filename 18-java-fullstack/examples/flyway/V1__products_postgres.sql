create extension if not exists pgcrypto;

create table app_users (
    id uuid primary key default gen_random_uuid(),
    email varchar(320) not null,
    password_hash varchar(255) not null,
    display_name varchar(120) not null,
    role varchar(30) not null default 'USER',
    enabled boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint uq_app_users_email unique (email),
    constraint ck_app_users_role check (role in ('USER', 'ADMIN'))
);

create table categories (
    id uuid primary key default gen_random_uuid(),
    name varchar(80) not null,
    slug varchar(100) not null,
    created_at timestamptz not null default now(),
    constraint uq_categories_slug unique (slug)
);

create table products (
    id uuid primary key default gen_random_uuid(),
    owner_id uuid not null references app_users(id),
    category_id uuid references categories(id),
    sku varchar(40) not null,
    name varchar(80) not null,
    description text,
    price numeric(12, 2) not null,
    currency char(3) not null,
    quantity integer not null,
    status varchar(20) not null default 'ACTIVE',
    version bigint not null default 0,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    constraint uq_products_owner_sku unique (owner_id, sku),
    constraint ck_products_price_non_negative check (price >= 0),
    constraint ck_products_quantity_non_negative check (quantity >= 0),
    constraint ck_products_status check (status in ('DRAFT', 'ACTIVE', 'ARCHIVED', 'DELETED'))
);

create table product_audit_events (
    id uuid primary key default gen_random_uuid(),
    product_id uuid not null references products(id),
    actor_user_id uuid references app_users(id),
    event_type varchar(60) not null,
    details jsonb not null default '{}'::jsonb,
    created_at timestamptz not null default now()
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
values
    ('Apparel', 'apparel'),
    ('Electronics', 'electronics'),
    ('Home', 'home'),
    ('Outdoor', 'outdoor')
on conflict (slug) do nothing;
