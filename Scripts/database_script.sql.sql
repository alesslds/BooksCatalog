CREATE DATABASE books_catalog
    WITH 
        OWNER = postgres
        ENCODING = 'UTF8'
        LC_COLLATE = 'en_US.utf8'
        LC_CTYPE = 'en_US.utf8'
        TEMPLATE = template0;



-- ============================================================================
-- 0. Extensión para UUID (usa gen_random_uuid())
-- ============================================================================
CREATE EXTENSION IF NOT EXISTS "pgcrypto";


-- ============================================================================
-- 1. Tabla principal: books
-- ============================================================================

CREATE TABLE IF NOT EXISTS books (
    id                uuid            NOT NULL DEFAULT gen_random_uuid(),
    title             varchar(200)    NOT NULL,
    author            varchar(200)    NOT NULL,
    publication_year  int             NOT NULL,
    publisher         varchar(200),
    page_count        int             NOT NULL,
    category          varchar(100),
    isbn              varchar(30),
    language          varchar(50),
    created_at        timestamptz     NOT NULL DEFAULT now(),
    updated_at        timestamptz     NOT NULL DEFAULT now(),
    version           int             NOT NULL DEFAULT 1,
    is_deleted        boolean         NOT NULL DEFAULT false,
    CONSTRAINT pk_books PRIMARY KEY (id),
    CONSTRAINT chk_books_page_count_positive CHECK (page_count > 0),
    CONSTRAINT chk_books_publication_year CHECK (publication_year >= 1400)
);

-- ============================================================================
-- 2. Índices
-- ============================================================================

-- Búsquedas rápidas por no eliminados
CREATE INDEX IF NOT EXISTS idx_books_is_deleted
    ON books (is_deleted);

-- Filtro frecuente por categoría
CREATE INDEX IF NOT EXISTS idx_books_category_not_deleted
    ON books (category)
    WHERE is_deleted = false;

-- Para búsquedas por título/autor/ISBN (case-insensitive)
CREATE INDEX IF NOT EXISTS idx_books_search_not_deleted
    ON books (LOWER(title), LOWER(author), LOWER(isbn))
    WHERE is_deleted = false;

-- Para ORDER BY title, id cuando is_deleted = false
CREATE INDEX IF NOT EXISTS idx_books_order_not_deleted
    ON books (is_deleted, title, id);

-- ============================================================================
-- 3. Stored procedure: crear libro
--      usp_books_create(...) RETURNS uuid
-- ============================================================================

CREATE OR REPLACE FUNCTION usp_books_create(
    p_title             text,
    p_author            text,
    p_publication_year  int,
    p_publisher         text,
    p_page_count        int,
    p_category          text,
    p_isbn              text,
    p_language          text
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_id uuid;
BEGIN
    INSERT INTO books (
        title,
        author,
        publication_year,
        publisher,
        page_count,
        category,
        isbn,
        language,
        created_at,
        updated_at,
        version,
        is_deleted
    )
    VALUES (
        trim(p_title),
        trim(p_author),
        p_publication_year,
        NULLIF(trim(p_publisher), ''),
        p_page_count,
        NULLIF(trim(p_category), ''),
        NULLIF(trim(p_isbn), ''),
        NULLIF(trim(p_language), ''),
        now(),
        now(),
        1,
        false
    )
    RETURNING id INTO v_id;

    RETURN v_id;
END;
$$;

-- ============================================================================
-- 4. Stored procedure: obtener libro por id
--      usp_books_get_by_id(p_id uuid)
-- ============================================================================

DROP FUNCTION IF EXISTS usp_books_get_by_id(uuid);
CREATE OR REPLACE FUNCTION usp_books_get_by_id(
    p_id uuid
)
RETURNS TABLE (
    id                 uuid,
    title              text,
    author             text,
    publication_year   int,
    publisher          text,
    page_count         int,
    category           text,
    isbn               text,
    language           text,
    created_at         timestamptz,
    updated_at         timestamptz,
    version            int
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT
        b.id,
        b.title::text,
        b.author::text,
        b.publication_year,
        b.publisher::text,
        b.page_count,
        b.category::text,
        b.isbn::text,
        b.language::text,
        b.created_at,
        b.updated_at,
        b.version
    FROM books b
    WHERE
        b.id = p_id
        AND b.is_deleted = false;
END;
$$;


-- ============================================================================
-- 5. Stored procedure: listado paginado
--      usp_books_list_paged(search, category, page_number, page_size)
-- ============================================================================
DROP FUNCTION IF EXISTS usp_books_list_paged(text, text, int, int);
CREATE OR REPLACE FUNCTION usp_books_list_paged(
    p_search        text,
    p_category      text,
    p_page_number   int,
    p_page_size     int
)
RETURNS TABLE (
    id                  uuid,
    title               text,
    author              text,
    publication_year    int,
    publisher           text,
    page_count          int,
    category            text,
    isbn                text,
    language            text,
    created_at          timestamptz,
    updated_at          timestamptz,
    version             int,
    total_count         bigint
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_page_number   int := GREATEST(p_page_number, 1);
    v_page_size     int := LEAST(GREATEST(p_page_size, 1), 100);
BEGIN
    RETURN QUERY
    SELECT
        b.id,
        b.title::text,
        b.author::text,
        b.publication_year,
        b.publisher::text,
        b.page_count,
        b.category::text,
        b.isbn::text,
        b.language::text,
        b.created_at,
        b.updated_at,
        b.version,
        COUNT(*) OVER() AS total_count
    FROM books b
    WHERE
        b.is_deleted = false
        AND (
            p_search IS NULL
            OR trim(p_search) = ''
            OR lower(b.title)  LIKE '%' || lower(trim(p_search)) || '%'
            OR lower(b.author) LIKE '%' || lower(trim(p_search)) || '%'
            OR lower(b.isbn)   LIKE '%' || lower(trim(p_search)) || '%'
        )
        AND (
            p_category IS NULL
            OR trim(p_category) = ''
            OR b.category = trim(p_category)
        )
    ORDER BY b.title ASC, b.id ASC
    OFFSET (v_page_number - 1) * v_page_size
    LIMIT v_page_size;
END;
$$;

-- ============================================================================
-- 6. Stored procedure: update con concurrencia optimista
--      usp_books_update(...) RETURNS int (número de filas afectadas)
-- ============================================================================

CREATE OR REPLACE FUNCTION usp_books_update(
    p_id                uuid,
    p_title             text,
    p_author            text,
    p_publication_year  int,
    p_publisher         text,
    p_page_count        int,
    p_category          text,
    p_isbn              text,
    p_language          text,
    p_expected_version  int
)
RETURNS int
LANGUAGE plpgsql
AS $$
DECLARE
    v_rows int;
BEGIN
    UPDATE books b
    SET
        title            = trim(p_title),
        author           = trim(p_author),
        publication_year = p_publication_year,
        publisher        = NULLIF(trim(p_publisher), ''),
        page_count       = p_page_count,
        category         = NULLIF(trim(p_category), ''),
        isbn             = NULLIF(trim(p_isbn), ''),
        language         = NULLIF(trim(p_language), ''),
        updated_at       = now(),
        version          = b.version + 1
    WHERE
        b.id = p_id
        AND b.is_deleted = false
        AND b.version = p_expected_version;

    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN v_rows;
END;
$$;

-- ============================================================================
-- 7. Stored procedure: soft delete
--      usp_books_soft_delete(p_id uuid) RETURNS int (filas afectadas)
-- ============================================================================

CREATE OR REPLACE FUNCTION usp_books_soft_delete(
    p_id uuid
)
RETURNS int
LANGUAGE plpgsql
AS $$
DECLARE
    v_rows int;
BEGIN
    UPDATE books b
    SET
        is_deleted = true,
        updated_at = now(),
        version    = b.version + 1
    WHERE
        b.id = p_id
        AND b.is_deleted = false;

    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN v_rows;
END;
$$;


-- ============================================================================
-- 8. Examples
-- ============================================================================


INSERT INTO books (
    title,
    author,
    publication_year,
    publisher,
    page_count,
    category,
    isbn,
    language
)
VALUES
-- 1
(
    'Clean Architecture',
    'Robert C. Martin',
    2017,
    'Pearson',
    432,
    'Software Engineering',
    '9780134494166',
    'en'
),
-- 2
(
    'Clean Code',
    'Robert C. Martin',
    2008,
    'Prentice Hall',
    464,
    'Software Engineering',
    '9780132350884',
    'en'
),
-- 3
(
    'The Pragmatic Programmer',
    'Andrew Hunt, David Thomas',
    1999,
    'Addison-Wesley',
    352,
    'Software Engineering',
    '9780201616224',
    'en'
),
-- 4
(
    'Domain-Driven Design: Tackling Complexity in the Heart of Software',
    'Eric Evans',
    2003,
    'Addison-Wesley',
    560,
    'Software Architecture',
    '9780321125217',
    'en'
),
-- 5
(
    'Refactoring: Improving the Design of Existing Code',
    'Martin Fowler',
    1999,
    'Addison-Wesley',
    448,
    'Software Engineering',
    '9780201485677',
    'en'
),
-- 6
(
    'Design Patterns: Elements of Reusable Object-Oriented Software',
    'Erich Gamma, Richard Helm, Ralph Johnson, John Vlissides',
    1994,
    'Addison-Wesley',
    395,
    'Software Design',
    '9780201633610',
    'en'
),
-- 7
(
    'Patterns of Enterprise Application Architecture',
    'Martin Fowler',
    2002,
    'Addison-Wesley',
    560,
    'Software Architecture',
    '9780321127426',
    'en'
),
-- 8
(
    'Working Effectively with Legacy Code',
    'Michael Feathers',
    2004,
    'Prentice Hall',
    456,
    'Software Engineering',
    '9780131177055',
    'en'
),
-- 9
(
    'Code Complete',
    'Steve McConnell',
    2004,
    'Microsoft Press',
    960,
    'Software Engineering',
    '9780735619678',
    'en'
),
-- 10
(
    'Test-Driven Development: By Example',
    'Kent Beck',
    2002,
    'Addison-Wesley',
    240,
    'Software Testing',
    '9780321146533',
    'en'
);
