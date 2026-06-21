alter table users add column content_id unsigned bigint;
create unique index users_content_id_idx on users (content_id);
